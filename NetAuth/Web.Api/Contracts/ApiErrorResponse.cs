namespace NetAuth.Web.Api.Contracts;

public sealed record ApiError(string Code, string Message);

public sealed record ApiErrorResponse(IReadOnlyCollection<ApiError> Errors);