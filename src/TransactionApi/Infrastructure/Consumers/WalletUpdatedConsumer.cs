using Marten;
using MassTransit;
using Shared.Contracts;
using TransactionApi.Infrastructure.Documents;
using TransactionApi.Infrastructure.Inbox;

namespace TransactionApi.Infrastructure.Consumers;

public class WalletUpdatedConsumer(IDocumentStore store) : IConsumer<WalletUpdatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<WalletUpdatedIntegrationEvent> context)
    {
        var msg = context.Message;
        var messageId = context.MessageId
            ?? throw new InvalidOperationException("Message is missing MessageId, cannot guarantee idempotency.");

        using var session = store.LightweightSession();

        if (await session.LoadAsync<InboxRecord>(messageId, context.CancellationToken) is not null)
            return;

        var current = session.LoadAsync<WalletReference>(msg.WalletId, context.CancellationToken).Result;

        if (current == null)
        {
            return;
        }

        session.Store(current with {  Name = msg.Name, WalletType = msg.WalletType});
        session.Store(new InboxRecord(messageId, DateTimeOffset.UtcNow));
        await session.SaveChangesAsync(context.CancellationToken);
    }
}
