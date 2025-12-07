using JetBrains.Annotations;
using MediatR;
using NetAuth.Application.TodoItems.Get;
using NetAuth.Web.Api.Contracts;

namespace NetAuth.Web.Api.Endpoints.TodoItems;

[UsedImplicitly]
internal sealed class GetTodoItemsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/todo-items", async (
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var query = new GetTodoItemsQuery();

                var result = await sender.Send(query, cancellationToken);

                return result
                    .Match(Right: Results.Ok, Left: CustomResults.Err);
            })
            .WithName("GetTodoItems")
            .WithSummary("Get todo items.")
            .WithDescription("Retrieves all todo items for the authenticated user.")
            .Produces<GetTodoItemsResult>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithTags(Tags.TodoItems)
            .RequireAuthorization("permission:todo-items:read");
    }
}