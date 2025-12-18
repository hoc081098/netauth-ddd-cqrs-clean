using Ardalis.GuardClauses;
using JetBrains.Annotations;
using LanguageExt;
using static LanguageExt.Prelude;
using NetAuth.Domain.Core.Abstractions;
using NetAuth.Domain.Core.Primitives;
using NetAuth.Domain.TodoItems.DomainEvents;
using NetAuth.Domain.Users;

namespace NetAuth.Domain.TodoItems;

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