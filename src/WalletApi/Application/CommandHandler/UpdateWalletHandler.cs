using Marten;
using MediatR;
using WalletApi.Application.Command;
using WalletApi.Domain;

namespace WalletApi.Application.CommandHandler;

public class UpdateWalletHandler(IDocumentStore documentStore) : IRequestHandler<UpdateWallet>
{
    public async Task Handle(UpdateWallet command, CancellationToken cancellationToken)
    {
        using var session = documentStore.LightweightSession();
        var aggregate = await session.Events.AggregateStreamAsync<WalletAggregate>(
            streamId: command.WalletId,
            token: cancellationToken);

        if (aggregate is null)
        {
            throw new KeyNotFoundException(message: $"Wallet '{command.WalletId}' was not found.");
        }

        var events = aggregate.UpdateWallet(
                walletId: command.WalletId,
                userId:command.UserId,
                name: command.NewName,
                walletType: command.NewType)
            .ToArray();

        if (events.Length == 0)
        {
            return;
        }

        session.Events.Append(stream: command.WalletId, events: events);
        await session.SaveChangesAsync(token: cancellationToken);
    }
}
