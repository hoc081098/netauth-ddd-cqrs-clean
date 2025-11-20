namespace NetAuth.Domain.Core.Primitives;

public sealed class DomainError(
    string code,
    string message
) : ValueObject
{
    public string Code { get; } = code;
    public string Message { get; } = message;

    protected override IEnumerable<object> GetAtomicValues() => [Code, Message];

    public override string ToString() => $"DomainError {{ {nameof(Code)}: {Code}, {nameof(Message)}: {Message} }}";
}