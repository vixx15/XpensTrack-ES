using Marten;
using MediatR;
using Shared.Contracts;
using Shared.Outbox;
using WalletApi.Application.Command;
using WalletApi.Domain;

namespace WalletApi.Application.CommandHandler;

public class UpdateWalletHandler(IDocumentStore documentStore) : IRequestHandler<UpdateWallet>
{
    public async Task Handle(UpdateWallet command, CancellationToken cancellationToken)
    {
        using var session = documentStore.LightweightSession();
        var stream = await session.Events.FetchForWriting<WalletAggregate>(
            command.WalletId, cancellationToken);

        var aggregate = stream.Aggregate
            ?? throw new KeyNotFoundException(message: $"Wallet '{command.WalletId}' was not found.");

        if (aggregate.UserId != command.UserId)
            throw new UnauthorizedAccessException(
                $"Wallet '{command.WalletId}' does not belong to user '{command.UserId}'.");

        var events = aggregate.UpdateWallet(
                walletId: command.WalletId,
                userId: command.UserId,
                name: command.NewName,
                walletType: command.NewType)
            .ToArray();

        if (events.Length == 0)
        {
            return;
        }

        stream.AppendMany(events);

        session.Store(OutboxMessage.From(new WalletUpdatedIntegrationEvent(
            WalletId: command.WalletId,
            UserId: command.UserId,
            Name: command.NewName,
            WalletType: command.NewType.ToString())));

        await session.SaveChangesAsync(token: cancellationToken);
    }
}
