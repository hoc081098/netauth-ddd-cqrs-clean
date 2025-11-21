namespace NetAuth.Domain.Core.Primitives;

public sealed class DomainError(
    string code,
    string message,
    DomainError.ErrorType type
) : ValueObject
{
    public enum ErrorType
    {
        Validation,
        NotFound,
        Conflict,
        Unauthorized,
        Forbidden,
        Failure
    }

    public string Code { get; } = code;
    public string Message { get; } = message;
    public ErrorType Type { get; } = type;

    protected override IEnumerable<object> GetAtomicValues() => [Code, Message, Type];

    public override string ToString() =>
        $"DomainError {{ {nameof(Code)}: {Code}, {nameof(Message)}: {Message}, ${nameof(Type)}: {Type} }}";
}