using Marten;
using MediatR;
using Shared;
using WalletApi.Application.Command;
using WalletApi.Domain;

namespace WalletApi.Application.CommandHandler;

public class CreateWalletHandler(IDocumentStore documentStore) : IRequestHandler<CreateWallet, Guid>
{
    public async Task<Guid> Handle(CreateWallet command, CancellationToken cancellationToken)
    {
        var walletId = Guid.NewGuid();
        using var session = documentStore.LightweightSession();

        var events = WalletAggregate.Create(
            walletId: walletId,
            name: command.Name,
            amount: new Money(value: command.Amount, currencyCode: command.CurrencyCode),
            walletType: command.WalletType,
            userId: command.UserId,
            defaultCurrencyExchangeRate: command.DefaultCurrencyExchangeRate,
            defaultCurrencyCode: command.DefaultCurrencyCode);

        session.Events.StartStream<WalletAggregate>(id: walletId, events: events);
        await session.SaveChangesAsync(token: cancellationToken);

        return walletId;
    }
}
