using JetBrains.Annotations;
using MediatR;
using NetAuth.Application.TodoItems.Update;
using NetAuth.Web.Api.Contracts;

namespace NetAuth.Web.Api.Endpoints.TodoItems;

[UsedImplicitly]
internal sealed class UpdateTodoItemEndpoint : IEndpoint
{
    [UsedImplicitly]
    public sealed record Request(
        string Title,
        string? Description,
        DateTimeOffset DueDate,
        IReadOnlyList<string> Labels);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/todo-items/{id:guid}", async (
                Guid id,
                Request request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateTodoItemCommand(
                    TodoItemId: id,
                    Title: request.Title,
                    Description: request.Description,
                    DueDate: request.DueDate,
                    Labels: request.Labels);

                var result = await sender.Send(command, cancellationToken);

                return result
                    .Match(Right: _ => Results.NoContent(), Left: CustomResults.Err);
            })
            .WithName("UpdateTodoItem")
            .WithSummary("Update a todo item.")
            .WithDescription("Updates an existing todo item for the authenticated user.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithTags(Tags.TodoItems)
            .RequireAuthorization("permission:todo-items:update");
    }
}