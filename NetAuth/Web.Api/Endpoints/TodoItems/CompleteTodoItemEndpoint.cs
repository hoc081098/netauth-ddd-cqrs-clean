using JetBrains.Annotations;
using MediatR;
using NetAuth.Application.TodoItems.Complete;
using NetAuth.Application.TodoItems.Create;
using NetAuth.Web.Api.Contracts;

namespace NetAuth.Web.Api.Endpoints.TodoItems;

[UsedImplicitly]
internal sealed class CompleteTodoItemEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/todo-items/{id:guid}/complete", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new CompleteTodoItemCommand(id);

                var result = await sender.Send(command, cancellationToken);

                return result
                    .Match(Right: _ => Results.NoContent(), Left: CustomResults.Err);
            })
            .WithName("CompleteTodoItem")
            .WithSummary("Complete a todo item.")
            .WithDescription("Marks the specified todo item as completed.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithTags(Tags.TodoItems)
            .RequireAuthorization("permission:todo-items:update");
    }
}