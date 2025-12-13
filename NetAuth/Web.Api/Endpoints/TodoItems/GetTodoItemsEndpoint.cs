using JetBrains.Annotations;
using MediatR;
using NetAuth.Application.TodoItems.Get;
using NetAuth.Web.Api.Contracts;

namespace NetAuth.Web.Api.Endpoints.TodoItems;

[UsedImplicitly]
internal sealed class GetTodoItemsEndpoint : IEndpoint
{
    [UsedImplicitly]
    public sealed record Response(IReadOnlyList<TodoItemResponse> TodoItems);

    [UsedImplicitly]
    public sealed record TodoItemResponse(
        Guid Id,
        string Title,
        string? Description,
        bool IsCompleted,
        DateTimeOffset? CompletedOnUtc,
        DateTimeOffset DueDateOnUtc,
        IReadOnlyList<string> Labels,
        DateTimeOffset CreatedOnUtc
    );

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/todo-items", async (
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var query = new GetTodoItemsQuery();

                var result = await sender.Send(query, cancellationToken);

                return result
                    .Map(ToResponse)
                    .Match(Right: Results.Ok, Left: CustomResults.Err);
            })
            .WithName("GetTodoItems")
            .WithSummary("Get todo items.")
            .WithDescription("Retrieves all todo items for the authenticated user.")
            .Produces<Response>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithTags(Tags.TodoItems)
            .RequireAuthorization("permission:todo-items:read");
    }

    private static Response ToResponse(GetTodoItemsResult result)
    {
        var items = result.TodoItems
            .Select(ToTodoItemResponse)
            .ToArray();

        return new Response(items);

        static TodoItemResponse ToTodoItemResponse(Application.TodoItems.Get.TodoItemResponse item) =>
            new(
                Id: item.Id,
                Title: item.Title,
                Description: item.Description,
                IsCompleted: item.IsCompleted,
                CompletedOnUtc: item.CompletedOnUtc,
                DueDateOnUtc: item.DueDateOnUtc,
                Labels: item.Labels,
                CreatedOnUtc: item.CreatedOnUtc
            );
    }
}