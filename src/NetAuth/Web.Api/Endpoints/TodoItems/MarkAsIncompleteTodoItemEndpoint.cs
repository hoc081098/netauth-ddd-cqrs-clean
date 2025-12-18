using JetBrains.Annotations;
using MediatR;
using NetAuth.Application.TodoItems.MarkAsIncomplete;
using NetAuth.Web.Api.Contracts;

namespace NetAuth.Web.Api.Endpoints.TodoItems;

[UsedImplicitly]
internal sealed class MarkAsIncompleteTodoItemEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/todo-items/{id:guid}/mark-as-incomplete", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new MarkAsIncompleteTodoItemCommand(id);

                var result = await sender.Send(command, cancellationToken);

                return result
                    .Match(Right: _ => Results.NoContent(), Left: CustomResults.Err);
            })
            .WithName("MarkAsIncompleteTodoItem")
            .WithSummary("Mark a todo item as incomplete.")
            .WithDescription("Marks the specified todo item as incomplete.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithTags(Tags.TodoItems)
            .RequireAuthorization("permission:todo-items:update");
    }
}
