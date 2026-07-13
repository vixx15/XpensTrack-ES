using Marten;
using MediatR;
using Shared.Contracts;
using TransactionApi.Application.Command;
using TransactionApi.Application.Interfaces;
using TransactionApi.Domain;
using TransactionApi.Domain.Events;
using TransactionApi.Domain.ValueObjects;
using TransactionApi.Infrastructure.Documents;
using Shared.Outbox;

namespace TransactionApi.Application.CommandHandler;

public class CreateTransactionHandler(
    IDocumentStore documentStore,
    IExchangeRateProvider exchangeRateProvider)
    : IRequestHandler<CreateTransaction, Guid>
{
    public async Task<Guid> Handle(CreateTransaction command, CancellationToken cancellationToken)
    {
        var transactionId = Guid.NewGuid();
        using var session = documentStore.LightweightSession();

        var wallet = await session.LoadAsync<WalletReference>(command.WalletId, cancellationToken)
            ?? throw new KeyNotFoundException($"Wallet '{command.WalletId}' not found.");

        if (wallet.UserId != command.UserId)
            throw new UnauthorizedAccessException($"Wallet '{command.WalletId}' does not belong to user '{command.UserId}'.");

        if (!string.Equals(wallet.CurrencyCode, command.CurrencyCode, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Transaction currency '{command.CurrencyCode}' does not match wallet currency '{wallet.CurrencyCode}'.");

        if (command.ToWalletId is not null)
        {
            var toWallet = await session.LoadAsync<WalletReference>(command.ToWalletId.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"Target wallet '{command.ToWalletId}' not found.");

            if (toWallet.UserId != command.UserId)
                throw new UnauthorizedAccessException($"Target wallet '{command.ToWalletId}' does not belong to user '{command.UserId}'.");
        }

        var amount = new Money(value: command.Amount, currencyCode: command.CurrencyCode);

        var defaultCurrencyExchangeRate =
            await exchangeRateProvider.GetRateAsync(command.CurrencyCode, command.DefaultCurrencyCode,
                command.OccuredAt, cancellationToken);

        decimal? toWalletExchangeRate = null;
        if (command.ToWalletCurrencyId is not null)
        {
            toWalletExchangeRate = await exchangeRateProvider.GetRateAsync(command.CurrencyCode,
                command.ToWalletCurrencyId, command.OccuredAt, cancellationToken);
        }

        var transferDetails = MapTransferDetails(
            transactionType: command.TransactionType,
            sourceWalletId: command.WalletId,
            sourceAmount: amount,
            toWalletId: command.ToWalletId,
            toWalletCurrencyCode: command.ToWalletCurrencyId,
            toWalletExchangeRate: toWalletExchangeRate);

        var events = TransactionAggregate.Create(
            transactionId: transactionId,
            userId: command.UserId,
            walletId: command.WalletId,
            amount: amount,
            categorization: new TransactionCategorization(
                type: command.TransactionType,
                category: command.Category),
            description: command.Description,
            occuredAt: command.OccuredAt,
            defaultCurrencyCode: command.DefaultCurrencyCode,
            defaultCurrencyExchangeRate: defaultCurrencyExchangeRate,
            transferDetails: transferDetails).ToList();
        session.Events.StartStream<TransactionAggregate>(id: transactionId, events: events);

        var createdEvent = events.OfType<TransactionCreated>().First();
        session.Store(OutboxMessage.From(new TransactionCreatedIntegrationEvent(
            createdEvent.TransactionId,
            createdEvent.WalletId,
            createdEvent.UserId,
            createdEvent.Amount.Value,
            createdEvent.Amount.CurrencyCode,
            createdEvent.TransactionType.ToString(),
            createdEvent.OccuredAt,
            createdEvent.DefaultCurrencyAmount.Value,
            createdEvent.DefaultCurrencyAmount.CurrencyCode,
            createdEvent.ToWalletId,
            createdEvent.ToWalletAmount?.Value,
            createdEvent.ToWalletAmount?.CurrencyCode,
            createdEvent.ToWalletCurrencyExchangeRate,
            createdEvent.ToWalletCurrencyCode)));

        await session.SaveChangesAsync(token: cancellationToken);
        
        return transactionId;
    }

    private static TransferDetails? MapTransferDetails(
        TransactionType transactionType,
        Guid sourceWalletId,
        Money sourceAmount,
        Guid? toWalletId,
        string? toWalletCurrencyCode,
        decimal? toWalletExchangeRate)
    {
        if (transactionType != TransactionType.Transfer)
        {
            if (toWalletId is not null ||
                !string.IsNullOrWhiteSpace(value: toWalletCurrencyCode) ||
                toWalletExchangeRate is not null)
            {
                throw new ArgumentException("Transfer fields are allowed only for transfer transactions.");
            }

            return null;
        }

        if (toWalletId is null)
        {
            throw new ArgumentException("Target wallet is required for transfer.");
        }

        if (toWalletId.Value == sourceWalletId)
        {
            throw new ArgumentException("Source and target wallet cannot be the same.");
        }

        if (string.IsNullOrWhiteSpace(value: toWalletCurrencyCode))
        {
            throw new ArgumentException("Target wallet currency is required for transfer.");
        }

        if (toWalletExchangeRate is null or <= 0)
        {
            throw new ArgumentException("Target wallet exchange rate must be greater than zero.");
        }

        var toWalletConversion = new CurrencyConversion(
            FromCurrencyCode: sourceAmount.CurrencyCode,
            ToCurrencyCode: toWalletCurrencyCode,
            ExchangeRate: toWalletExchangeRate.Value);

        return new TransferDetails(
            ToWalletId: toWalletId.Value,
            ToWalletConversion: toWalletConversion);
    }
}