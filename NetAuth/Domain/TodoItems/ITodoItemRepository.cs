namespace NetAuth.Domain.TodoItems;

public interface ITodoItemRepository
{
    Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    IQueryable<TodoItem> GetTodoItemsByUserId(Guid userId);

    void Insert(TodoItem todoItem);

    void Update(TodoItem todoItem);

    void Remove(TodoItem todoItem);
}