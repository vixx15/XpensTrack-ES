using Marten;
using MediatR;
using Shared;
using TransactionApi.Application.Command;
using TransactionApi.Domain;
using TransactionApi.Domain.ValueObjects;

namespace TransactionApi.Application.CommandHandler;

public class CreateTransactionHandler(IDocumentStore documentStore) : IRequestHandler<CreateTransaction, Guid>
{
    public async Task<Guid> Handle(CreateTransaction command, CancellationToken cancellationToken)
    {
        //TODO wallet existance check
        var transactionId = Guid.NewGuid();
        using var session = documentStore.LightweightSession();

        var amount = new Money(value: command.Amount, currencyCode: command.CurrencyCode);

        var transferDetails = MapTransferDetails(
            transactionType: command.TransactionType,
            sourceWalletId: command.WalletId,
            sourceAmount: amount,
            toWalletId: command.ToWalletId,
            toWalletCurrencyCode: command.ToWalletCurrencyId,
            toWalletExchangeRate: command.ToWalletCurrencyExchangeRate);

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
            defaultCurrencyExchangeRate: command.DefaultCurrencyExchangeRate,
            transferDetails: transferDetails);
        session.Events.StartStream<TransactionAggregate>(id: transactionId, events: events);
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