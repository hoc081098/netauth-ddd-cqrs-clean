using Ardalis.GuardClauses;
using JetBrains.Annotations;
using LanguageExt;
using NetAuth.Domain.Core.Abstractions;
using NetAuth.Domain.Core.Primitives;
using NetAuth.Domain.TodoItems.DomainEvents;

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
        Guard.Against.Default(dueDateOnUtc);
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

    public static Either<DomainError, TodoItem> Create(
        Guid userId,
        string title,
        string? description,
        DateTimeOffset dueDateOnUtc,
        IReadOnlyList<string> labels
    )
    {
        var todoItemEither = from todoTitle in TodoTitle.Create(title)
            from todoDescription in TodoDescription.Create(description ?? string.Empty)
            select new TodoItem(
                id: Guid.CreateVersion7(),
                userId: userId,
                title: todoTitle,
                description: todoDescription,
                dueDateOnUtc: dueDateOnUtc,
                labels: labels
            );

        todoItemEither.IfRight(item =>
            item.AddDomainEvent(
                new TodoItemCreatedDomainEvent(
                    TodoItemId: item.Id,
                    UserId: item.UserId)
            )
        );

        return todoItemEither;
    }

    public Either<DomainError, Unit> Update(
        TodoTitle title,
        TodoDescription? description,
        DateTimeOffset dueDateOnUtc,
        IReadOnlyList<string> labels)
    {
        if (IsCompleted)
        {
            return TodoItemDomainErrors.TodoItem.CannotUpdateCompletedItem;
        }

        Title = title;
        Description = description;
        DueDateOnUtc = dueDateOnUtc;
        Labels = [..labels];

        return Unit.Default;
    }

    public Either<DomainError, Unit> MarkAsCompleted(DateTimeOffset currentUtc)
    {
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
}