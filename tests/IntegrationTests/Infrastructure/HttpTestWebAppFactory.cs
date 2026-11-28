using JetBrains.Annotations;

namespace NetAuth.IntegrationTests.Infrastructure;

/// <summary>
/// Web app factory that keeps the production JWT authentication, claims transformation
/// and permission-based authorization, for testing the real HTTP security boundary.
/// </summary>
[UsedImplicitly]
public sealed class HttpTestWebAppFactory : IntegrationTestWebAppFactory
{
    protected override bool UseRealUserContext => true;
}
