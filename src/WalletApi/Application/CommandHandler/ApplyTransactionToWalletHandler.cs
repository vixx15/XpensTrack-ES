using Marten;
using MediatR;
using WalletApi.Application.Command;
using WalletApi.Domain;
using WalletApi.Infrastructure.Inbox;

namespace WalletApi.Application.CommandHandler;

public class ApplyTransactionToWalletHandler(IDocumentStore documentStore)
    : IRequestHandler<ApplyTransactionToWallet>
{
    public async Task Handle(ApplyTransactionToWallet command, CancellationToken ct)
    {
        using var session = documentStore.LightweightSession();

        if (await session.LoadAsync<InboxRecord>(command.MessageId, ct) is not null)
            return;

        var stream = await FetchForWriting(session, command.WalletId, ct);
        stream.AppendMany(stream.Aggregate!.ApplyTransaction(
            command.UserId, command.TransactionId,
            command.Amount, command.DefaultCurrencyAmount, command.Direction, command.OccurredAt));

        if (command.Transfer is { } transfer)
        {
            if (transfer.WalletId == command.WalletId)
                throw new InvalidOperationException(
                    $"Transfer target wallet '{transfer.WalletId}' cannot be the same as source wallet.");

            var toStream = await FetchForWriting(session, transfer.WalletId, ct);
            toStream.AppendMany(toStream.Aggregate!.ApplyTransaction(
                command.UserId, command.TransactionId,
                transfer.Amount, transfer.DefaultCurrencyAmount, BalanceDirection.Added, command.OccurredAt));
        }

        session.Store(new InboxRecord(command.MessageId, DateTimeOffset.UtcNow));
        await session.SaveChangesAsync(ct);
    }

    private static async Task<Marten.Events.IEventStream<WalletAggregate>> FetchForWriting(
        IDocumentSession session, Guid walletId, CancellationToken ct)
    {
        var stream = await session.Events.FetchForWriting<WalletAggregate>(walletId, ct);
        if (stream.Aggregate is null)
            throw new KeyNotFoundException($"Wallet '{walletId}' was not found.");
        return stream;
    }
}
