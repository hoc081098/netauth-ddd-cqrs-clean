using Microsoft.EntityFrameworkCore;
using NetAuth.Domain.TodoItems;

namespace NetAuth.Infrastructure.Repositories;

internal sealed class TodoItemRepository(AppDbContext dbContext) :
    GenericRepository<Guid, TodoItem>(dbContext),
    ITodoItemRepository
{
    public async Task<IReadOnlyList<TodoItem>> GetTodoItemsByUserId(
        Guid userId,
        CancellationToken cancellationToken = default
    ) =>
        await DbContext.TodoItems
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedOnUtc)
            .ToListAsync(cancellationToken);
}