namespace NetAuth.Domain.TodoItems;

public interface ITodoItemRepository
{
    Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TodoItem>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    void Insert(TodoItem todoItem);

    void Update(TodoItem todoItem);

    void Remove(TodoItem todoItem);
}