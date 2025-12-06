using System.Diagnostics.Contracts;
using LanguageExt;
using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Domain.TodoItems;

public sealed class TodoDescription : ValueObject
{
    public const int MaxLength = 500;

    /// <summary>
    /// Prevents direct instantiation from outside the class.
    /// This ensures that all instances are created through the Create method, enforcing invariants.
    /// </summary>
    private TodoDescription()
    {
    }

    /// <summary>
    /// The value of the todo description.
    /// It can be empty or blank.
    /// </summary>
    public required string Value { get; init; }

    [Pure]
    public static Either<DomainError, TodoDescription?> Create(string description) =>
        description switch
        {
            { Length: > MaxLength } => TodoItemDomainErrors.Description.TooLong,
            _ => new TodoDescription { Value = description }
        };

    protected override IEnumerable<object> GetAtomicValues() => [Value];

    public static implicit operator string(TodoDescription description) => description.Value;
}