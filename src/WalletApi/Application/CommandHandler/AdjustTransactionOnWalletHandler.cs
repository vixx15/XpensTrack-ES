using Marten;
using MediatR;
using WalletApi.Application.Command;
using WalletApi.Domain;
using WalletApi.Infrastructure.Inbox;

namespace WalletApi.Application.CommandHandler;

public class AdjustTransactionOnWalletHandler(IDocumentStore documentStore)
    : IRequestHandler<AdjustTransactionOnWallet>
{
    public async Task Handle(AdjustTransactionOnWallet command, CancellationToken ct)
    {
        using var session = documentStore.LightweightSession();

        if (await session.LoadAsync<InboxRecord>(command.MessageId, ct) is not null)
            return;

        await AdjustMainWallet(session, command, ct);
        await AdjustTransferWallet(session, command, ct);

        session.Store(new InboxRecord(command.MessageId, DateTimeOffset.UtcNow));
        await session.SaveChangesAsync(ct);
    }

    private static async Task AdjustMainWallet(
        IDocumentSession session, AdjustTransactionOnWallet cmd, CancellationToken ct)
    {
        if (cmd.OldWalletId == cmd.NewWalletId)
        {
            var stream = await FetchForWriting(session, cmd.NewWalletId, ct);
            stream.AppendMany(stream.Aggregate!.AdjustTransaction(
                cmd.UserId,
                cmd.TransactionId,
                cmd.OldAmount,
                cmd.OldDefaultCurrencyAmount,
                cmd.OldDirection,
                cmd.NewAmount,
                cmd.NewDefaultCurrencyAmount,
                cmd.NewDirection,
                cmd.NewOccurredAt));
        }
        else
        {
            var oldStream = await FetchForWriting(session, cmd.OldWalletId, ct);
            oldStream.AppendMany(oldStream.Aggregate!.RevertTransaction(
                cmd.UserId, cmd.TransactionId,
                cmd.OldAmount, cmd.OldDefaultCurrencyAmount, cmd.OldDirection, cmd.OldOccurredAt));

            var newStream = await FetchForWriting(session, cmd.NewWalletId, ct);
            newStream.AppendMany(newStream.Aggregate!.ApplyTransaction(
                cmd.UserId, cmd.TransactionId,
                cmd.NewAmount, cmd.NewDefaultCurrencyAmount, cmd.NewDirection, cmd.NewOccurredAt));
        }
    }

    private static async Task AdjustTransferWallet(
        IDocumentSession session, AdjustTransactionOnWallet cmd, CancellationToken ct)
    {
        var old = cmd.OldTransfer;
        var @new = cmd.NewTransfer;

        if (old == null && @new == null) return;

        if (old != null && old.WalletId == cmd.OldWalletId)
            throw new InvalidOperationException(
                $"Transfer target wallet '{old.WalletId}' cannot be the same as source wallet.");

        if (@new != null && @new.WalletId == cmd.NewWalletId)
            throw new InvalidOperationException(
                $"Transfer target wallet '{@new.WalletId}' cannot be the same as source wallet.");

        if (old != null && @new != null && old.WalletId == @new.WalletId)
        {
            var stream = await FetchForWriting(session, @new.WalletId, ct);
            stream.AppendMany(stream.Aggregate!.AdjustTransaction(
                cmd.UserId, cmd.TransactionId,
                old.Amount, old.DefaultCurrencyAmount, BalanceDirection.Added,
                @new.Amount, @new.DefaultCurrencyAmount, BalanceDirection.Added,
                cmd.NewOccurredAt));
        }
        else
        {
            if (old != null)
            {
                var oldToStream = await FetchForWriting(session, old.WalletId, ct);
                oldToStream.AppendMany(oldToStream.Aggregate!.RevertTransaction(
                    cmd.UserId, cmd.TransactionId,
                    old.Amount, old.DefaultCurrencyAmount, BalanceDirection.Added, cmd.OldOccurredAt));
            }

            if (@new != null)
            {
                var newToStream = await FetchForWriting(session, @new.WalletId, ct);
                newToStream.AppendMany(newToStream.Aggregate!.ApplyTransaction(
                    cmd.UserId, cmd.TransactionId,
                    @new.Amount, @new.DefaultCurrencyAmount, BalanceDirection.Added, cmd.NewOccurredAt));
            }
        }
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