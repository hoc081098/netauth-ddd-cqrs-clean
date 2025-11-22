namespace NetAuth.Web.Api.Contracts;

public sealed record ApiErrorResponse(string Code, string Message, string ErrorType);
