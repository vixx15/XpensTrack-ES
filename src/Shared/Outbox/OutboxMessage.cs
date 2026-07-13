using System.Text.Json;

namespace Shared.Outbox;

public class OutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string MessageType { get; init; }
    public required string Payload { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public static OutboxMessage From<T>(T message) where T : notnull => new()
    {
        MessageType = typeof(T).Name,
        Payload = JsonSerializer.Serialize(message)
    };
}
