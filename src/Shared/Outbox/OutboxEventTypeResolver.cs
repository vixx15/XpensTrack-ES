using System.Text.Json;

namespace Shared.Outbox;

public static class OutboxEventTypeResolver
{
    public static object? Deserialize(OutboxMessage message)
    {
        var type = typeof(OutboxEventTypeResolver).Assembly
            .GetType($"Shared.Contracts.{message.MessageType}");

        return type is null ? null : JsonSerializer.Deserialize(message.Payload, type);
    }
}
