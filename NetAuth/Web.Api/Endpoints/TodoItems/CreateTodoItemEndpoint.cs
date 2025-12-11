using JetBrains.Annotations;
using MediatR;
using NetAuth.Application.TodoItems.Create;
using NetAuth.Web.Api.Contracts;
using NetAuth.Web.Api.OpenApi;

namespace NetAuth.Web.Api.Endpoints.TodoItems;

[UsedImplicitly]
internal sealed class CreateTodoItemEndpoint : IEndpoint
{
    [SwaggerRequired]
    [UsedImplicitly]
    public sealed record Request(
        string Title,
        string? Description,
        DateTimeOffset DueDate,
        IReadOnlyList<string> Labels);

    [UsedImplicitly]
    public sealed record Response(Guid Id);

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
                    .Map(r => new Response(r.TodoItemId))
                    .Match(Right: response =>
                            Results.Created(
                                uri: $"/todo-items/{response.Id}",
                                value: response),
                        Left: CustomResults.Err
                    );
            })
            .WithName("CreateTodoItem")
            .WithSummary("Create a new todo item.")
            .WithDescription("Creates a new todo item for the authenticated user.")
            .Produces<Response>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithTags(Tags.TodoItems)
            .RequireAuthorization("permission:todo-items:create");
    }
}