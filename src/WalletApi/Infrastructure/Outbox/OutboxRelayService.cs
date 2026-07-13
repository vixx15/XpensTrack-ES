using Marten;
using MassTransit;
using Shared.Outbox;

namespace WalletApi.Infrastructure.Outbox;

public class OutboxRelayService(IDocumentStore store, IBus bus, ILogger<OutboxRelayService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessages(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox relay failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessPendingMessages(CancellationToken ct)
    {
        IReadOnlyList<OutboxMessage> messages;
        using (var querySession = store.QuerySession())
        {
            messages = await querySession.Query<OutboxMessage>()
                .OrderBy(m => m.CreatedAt)
                .Take(50)
                .ToListAsync(ct);
        }

        foreach (var message in messages)
        {
            var integrationEvent = OutboxEventTypeResolver.Deserialize(message);
            if (integrationEvent is null)
            {
                logger.LogWarning("Unknown outbox message type {Type}, skipping", message.MessageType);
            }
            else
            {
                await bus.Publish(integrationEvent, ct);
            }

            using var deleteSession = store.LightweightSession();
            deleteSession.Delete<OutboxMessage>(message.Id);
            await deleteSession.SaveChangesAsync(ct);
        }
    }
}
