using System.Diagnostics.Contracts;
using Ardalis.GuardClauses;
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
    public static Either<DomainError, TodoDescription> Create(string description)
    {
        Guard.Against.Null(description); // If description is null, use CreateOption instead.

        return description switch
        {
            { Length: > MaxLength } =>
                TodoItemDomainErrors.Description.TooLong,
            _ => new TodoDescription { Value = description }
        };
    }

    [Pure]
    public static Either<DomainError, Option<TodoDescription>> CreateOption(string? description) =>
        description is null
            ? Right<Option<TodoDescription>>(None)
            : Create(description).Map(Some);

    protected override IEnumerable<object> GetAtomicValues() => [Value];

    public static implicit operator string(TodoDescription description) => description.Value;
}