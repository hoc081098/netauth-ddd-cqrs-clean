using LanguageExt;
using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Domain.TodoItems;

public sealed class TodoTitle : ValueObject
{
    public const int MaxLength = 100;

    /// <summary>
    /// Prevents direct instantiation from outside the class.
    /// This ensures that all instances are created through the Create method, enforcing invariants.
    /// </summary>
    private TodoTitle()
    {
    }

    public required string Value { get; init; }

    public static Either<DomainError, TodoTitle> Create(string title) =>
        title switch
        {
            _ when string.IsNullOrWhiteSpace(title) =>
                TodoItemDomainErrors.Title.NullOrEmpty,
            { Length: > MaxLength } =>
                TodoItemDomainErrors.Title.TooLong,
            _ => new TodoTitle { Value = title }
        };

    public static implicit operator string(TodoTitle title) => title.Value;

    protected override IEnumerable<object> GetAtomicValues() => [Value];
}