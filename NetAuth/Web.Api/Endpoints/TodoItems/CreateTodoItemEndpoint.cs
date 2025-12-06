using JetBrains.Annotations;
using MediatR;
using NetAuth.Application.TodoItems.CreateTodoItem;
using NetAuth.Web.Api.Contracts;
using static LanguageExt.Prelude;

namespace NetAuth.Web.Api.Endpoints.TodoItems;

[UsedImplicitly]
internal sealed class CreateTodoItemEndpoint : IEndpoint
{
    public sealed record Request(
        string Title,
        string? Description,
        DateTimeOffset DueDate,
        IReadOnlyList<string> Labels);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/todo-items", async (
                Request request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateTodoItemCommand(
                    Title: request.Title,
                    Description: request.Description,
                    DueDate: request.DueDate,
                    Labels: request.Labels);

                var result = await sender.Send(command, cancellationToken);

                return result
                    .Match(Right: r =>
                            Results.Created(
                                uri: $"/todo-items/{r.TodoItemId}",
                                value: new { id = r.TodoItemId }),
                        Left: CustomResults.Err
                    );
            })
            .WithName("CreateTodoItem")
            .WithSummary("Create a new todo item.")
            .WithDescription("Creates a new todo item for the authenticated user.")
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .RequireAuthorization("permissions:todo-items:create");
    }
}