using System.Diagnostics.Contracts;
using LanguageExt;
using static LanguageExt.Prelude;
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

    public required string Value { get; init; }

    [Pure]
    public static Either<DomainError, TodoDescription> Create(string description) =>
        description switch
        {
            _ when string.IsNullOrWhiteSpace(description) =>
                TodoItemDomainErrors.Description.NullOrEmpty,
            { Length: > MaxLength } =>
                TodoItemDomainErrors.Description.TooLong,
            _ => new TodoDescription { Value = description }
        };

    [Pure]
    public static Either<DomainError, Option<TodoDescription>> CreatOption(string? description) =>
        description is null
            ? Right(new Option<TodoDescription>())
            : Create(description).Map(Some);

    protected override IEnumerable<object> GetAtomicValues() => [Value];

    public static implicit operator string(TodoDescription description) => description.Value;
}