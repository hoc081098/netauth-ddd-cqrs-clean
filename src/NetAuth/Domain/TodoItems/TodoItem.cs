using Ardalis.GuardClauses;
using JetBrains.Annotations;
using LanguageExt;
using static LanguageExt.Prelude;
using NetAuth.Domain.Core.Abstractions;
using NetAuth.Domain.Core.Primitives;
using NetAuth.Domain.TodoItems.DomainEvents;
using NetAuth.Domain.Users;

namespace NetAuth.Domain.TodoItems;

/// <summary>
/// Represents a to-do item in the system. This is the aggregate root for to-do item operations.
/// </summary>
/// <remarks>
/// <para>
/// The TodoItem aggregate manages:
/// <list type="bullet">
/// <item><description>To-do item content (title, description)</description></item>
/// <item><description>Completion status and timestamp</description></item>
/// <item><description>Due date with validation (must be UTC and not in the past)</description></item>
/// <item><description>Labels for categorization</description></item>
/// <item><description>Ownership (linked to a user)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Invariants:</b>
/// <list type="bullet">
/// <item><description>Title is required and must be valid</description></item>
/// <item><description>Due date must be in UTC and not be in the past when created or updated</description></item>
/// <item><description>Completed items cannot be updated</description></item>
/// <item><description>Cannot mark an already completed item as completed</description></item>
/// <item><description>Cannot mark an incomplete item as incomplete</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class TodoItem : AggregateRoot<Guid>, IAuditableEntity, ISoftDeletableEntity
{
    /// <remarks>Required by EF Core.</remarks>
    [UsedImplicitly]
    private TodoItem()
    {
    }

    private TodoItem(Guid id,
        Guid userId,
        TodoTitle title,
        TodoDescription? description,
        DateTimeOffset dueDateOnUtc,
        IReadOnlyList<string> labels
    ) : base(id)
    {
        Guard.Against.Default(userId);
        Guard.Against.Null(title);
        Guard.Against.Null(labels);

        UserId = userId;
        Title = title;
        Description = description;
        IsCompleted = false;
        CompletedOnUtc = null;
        DueDateOnUtc = dueDateOnUtc;
        Labels = [..labels];
    }

    public Guid UserId { get; init; }

    public TodoTitle Title { get; private set; } = null!;

    public TodoDescription? Description { get; private set; }

    public bool IsCompleted { get; private set; }

    public DateTimeOffset? CompletedOnUtc { get; private set; }

    public DateTimeOffset DueDateOnUtc { get; private set; }

    public IReadOnlyList<string> Labels { get; private set; } = null!;

    /// <inheritdoc />
    [UsedImplicitly]
    public DateTimeOffset CreatedOnUtc { get; }

    /// <inheritdoc />
    [UsedImplicitly]
    public DateTimeOffset? ModifiedOnUtc { get; }

    /// <inheritdoc />
    [UsedImplicitly]
    public DateTimeOffset? DeletedOnUtc { get; }

    /// <inheritdoc />
    [UsedImplicitly]
    public bool IsDeleted { get; }

    // Navigational property
    public User User { get; private set; } = null!;

    /// <summary>
    /// Creates a new to-do item with validation.
    /// </summary>
    /// <param name="userId">The ID of the user who owns this item.</param>
    /// <param name="title">The title of the to-do item (required, max 100 characters).</param>
    /// <param name="description">Optional description (max 500 characters).</param>
    /// <param name="dueDateOnUtc">The due date in UTC (zero offset, must not be before <paramref name="currentUtc"/>).</param>
    /// <param name="labels">List of labels for categorization.</param>
    /// <param name="currentUtc">The current UTC time for due date validation.</param>
    /// <returns>
    /// An Either containing the created <see cref="TodoItem"/> on success,
    /// or a <see cref="DomainError"/> if validation fails.
    /// </returns>
    /// <remarks>
    /// This method validates:
    /// <list type="bullet">
    /// <item><description>Title format and length (up to 100 characters)</description></item>
    /// <item><description>Description format and length (if provided, up to 500 characters)</description></item>
    /// <item><description>Due date is not in the past and is UTC</description></item>
    /// </list>
    /// On success, raises a <see cref="TodoItemCreatedDomainEvent"/>.
    /// </remarks>
    public static Either<DomainError, TodoItem> Create(
        Guid userId,
        string title,
        string? description,
        DateTimeOffset dueDateOnUtc,
        IReadOnlyList<string> labels,
        DateTimeOffset currentUtc
    ) =>
        (from todoTitle in TodoTitle.Create(title)
            from todoDescription in TodoDescription.CreateOption(description)
            from validDueDate in ValidateDueDate(dueDateOnUtc, currentUtc)
            select new TodoItem(
                id: Guid.CreateVersion7(),
                userId: userId,
                title: todoTitle,
                description: todoDescription.MatchUnsafe(Some: identity, None: () => null),
                dueDateOnUtc: validDueDate,
                labels: labels
            )).Map(item =>
            {
                item.AddDomainEvent(
                    new TodoItemCreatedDomainEvent(
                        TodoItemId: item.Id,
                        UserId: item.UserId)
                );
                return item;
            }
        );

    /// <summary>
    /// Updates the to-do item with new values.
    /// </summary>
    /// <param name="title">The new title.</param>
    /// <param name="description">The new description (or null to clear).</param>
    /// <param name="dueDateOnUtc">The new due date (must be UTC with zero offset and not in the past).</param>
    /// <param name="labels">The new list of labels.</param>
    /// <param name="currentUtc">The current UTC time for due date validation.</param>
    /// <returns>
    /// <see cref="Unit.Default"/> on success, or a <see cref="DomainError"/> if:
    /// <list type="bullet">
    /// <item><description>The item is already completed (<see cref="TodoItemDomainErrors.TodoItem.CannotUpdateCompletedItem"/>)</description></item>
    /// <item><description>The due date is in the past or not UTC (<see cref="TodoItemDomainErrors.TodoItem.DueDateInPast"/>)</description></item>
    /// </list>
    /// </returns>
    public Either<DomainError, Unit> Update(
        TodoTitle title,
        TodoDescription? description,
        DateTimeOffset dueDateOnUtc,
        IReadOnlyList<string> labels,
        DateTimeOffset currentUtc
    )
    {
        Guard.Against.Null(title);
        Guard.Against.Null(labels);

        if (IsCompleted)
        {
            return TodoItemDomainErrors.TodoItem.CannotUpdateCompletedItem;
        }

        return ValidateDueDate(dueDateOnUtc, currentUtc)
            .Map(validDueDateOnUtc =>
            {
                Title = title;
                Description = description;
                DueDateOnUtc = validDueDateOnUtc;
                Labels = [..labels];

                return Unit.Default;
            });
    }

    /// <summary>
    /// Marks the to-do item as completed.
    /// </summary>
    /// <param name="currentUtc">The current UTC time to record as completion time.</param>
    /// <returns>
    /// <see cref="Unit.Default"/> on success, or <see cref="TodoItemDomainErrors.TodoItem.AlreadyCompleted"/>
    /// if the item is already completed.
    /// </returns>
    /// <remarks>
    /// On success, raises a <see cref="TodoItemCompletedDomainEvent"/>.
    /// </remarks>
    public Either<DomainError, Unit> MarkAsCompleted(DateTimeOffset currentUtc)
    {
        Guard.Against.Default(currentUtc);

        if (IsCompleted)
        {
            return TodoItemDomainErrors.TodoItem.AlreadyCompleted;
        }

        IsCompleted = true;
        CompletedOnUtc = currentUtc;
        AddDomainEvent(new TodoItemCompletedDomainEvent(TodoItemId: Id, UserId: UserId));

        return Unit.Default;
    }

    /// <summary>
    /// Marks the to-do item as incomplete (undoes completion).
    /// </summary>
    /// <returns>
    /// <see cref="Unit.Default"/> on success, or <see cref="TodoItemDomainErrors.TodoItem.NotCompleted"/>
    /// if the item is not currently completed.
    /// </returns>
    public Either<DomainError, Unit> MarkAsIncomplete()
    {
        if (!IsCompleted)
        {
            return TodoItemDomainErrors.TodoItem.NotCompleted;
        }

        IsCompleted = false;
        CompletedOnUtc = null;

        return Unit.Default;
    }

    private static Either<DomainError, DateTimeOffset> ValidateDueDate(
        DateTimeOffset dueDateOnUtc,
        DateTimeOffset currentUtc
    ) =>
        // Due date must be now or later, and in UTC
        dueDateOnUtc >= currentUtc && dueDateOnUtc.Offset == TimeSpan.Zero
            ? Right(dueDateOnUtc)
            : TodoItemDomainErrors.TodoItem.DueDateInPast;
}
