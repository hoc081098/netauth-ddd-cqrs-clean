using JetBrains.Annotations;

namespace NetAuth.Web.Api.Endpoints;

[UsedImplicitly]
public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}