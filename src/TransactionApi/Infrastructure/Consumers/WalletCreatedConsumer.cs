using Marten;
using MassTransit;
using Shared.Contracts;
using TransactionApi.Infrastructure.Documents;
using TransactionApi.Infrastructure.Inbox;

namespace TransactionApi.Infrastructure.Consumers;

public class WalletCreatedConsumer(IDocumentStore store) : IConsumer<WalletCreatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<WalletCreatedIntegrationEvent> context)
    {
        var msg = context.Message;
        var messageId = context.MessageId
            ?? throw new InvalidOperationException("Message is missing MessageId, cannot guarantee idempotency.");

        using var session = store.LightweightSession();

        if (await session.LoadAsync<InboxRecord>(messageId, context.CancellationToken) is not null)
            return;

        session.Store(new WalletReference { Id = msg.WalletId, Name = msg.Name, WalletType = msg.WalletType, UserId = msg.UserId, CurrencyCode = msg.CurrencyCode });
        session.Store(new InboxRecord(messageId, DateTimeOffset.UtcNow));
        await session.SaveChangesAsync(context.CancellationToken);
    }
}
