using Marten;
using MediatR;
using Shared.Contracts;
using Shared.Outbox;
using WalletApi.Application.Command;
using WalletApi.Application.Interfaces;
using WalletApi.Domain;
using WalletApi.Domain.Events;
using WalletApi.Domain.ValueObjects;

namespace WalletApi.Application.CommandHandler;

public class CreateWalletHandler(
    IDocumentStore documentStore,
    IExchangeRateProvider exchangeRateProvider) : IRequestHandler<CreateWallet, Guid>
{
    public async Task<Guid> Handle(CreateWallet command, CancellationToken cancellationToken)
    {
        var walletId = Guid.NewGuid();
        using var session = documentStore.LightweightSession();

        var defaultCurrencyExchangeRate = await exchangeRateProvider.GetRateAsync(command.CurrencyCode,
            command.DefaultCurrencyCode, cancellationToken: cancellationToken);

        var events = WalletAggregate.Create(
            walletId: walletId,
            name: command.Name,
            openingBalance: new Money(value: command.OpeningBalance, currencyCode: command.CurrencyCode),
            walletType: command.WalletType,
            userId: command.UserId,
            defaultCurrencyExchangeRate: defaultCurrencyExchangeRate,
            defaultCurrencyCode: command.DefaultCurrencyCode);

        var eventList = events.ToList();
        session.Events.StartStream<WalletAggregate>(id: walletId, events: eventList);

        var createdEvent = eventList.OfType<WalletCreated>().First();
        session.Store(OutboxMessage.From(new WalletCreatedIntegrationEvent(
            WalletId: createdEvent.WalletId,
            Name: createdEvent.Name,
            CurrencyCode: createdEvent.OpeningBalance.CurrencyCode,
            WalletType: createdEvent.WalletType.ToString(),
            UserId: createdEvent.UserId)));

        await session.SaveChangesAsync(token: cancellationToken);

        return walletId;
    }
}