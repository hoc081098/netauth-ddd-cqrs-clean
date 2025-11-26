using System.Collections.Concurrent;
using System.Text.Json;
using Ardalis.GuardClauses;
using LanguageExt;

namespace NetAuth.Infrastructure.Outbox;

internal interface IOutboxMessageResolver
{
    internal Fin<object> DeserializeEvent(string type, string content);
}

internal sealed class OutboxMessageResolver : IOutboxMessageResolver
{
    private static readonly ConcurrentDictionary<string, Type> TypeCache = new();

    public Fin<object> DeserializeEvent(string type, string content)
    {
        try
        {
            var messageType = GetOrAddMessageType(type);
            var deserialized = JsonSerializer.Deserialize(content, messageType);

            Guard.Against.Null(deserialized,
                exceptionCreator: () => new InvalidOperationException(
                    $"Deserialized outbox message content is null for type '{type}'."));

            return Fin<object>.Succ(deserialized);
        }
        catch (Exception e)
        {
            return Fin<object>.Fail(e);
        }
    }

    private static Type GetOrAddMessageType(string typename) =>
        TypeCache.GetOrAdd(typename,
            k =>
                Guard.Against.Null(
                    Domain.AssemblyReference.Assembly.GetType(k),
                    exceptionCreator: () =>
                        new InvalidOperationException($"Failed to get outbox message type '{k}' from assembly.")
                )
        );
}