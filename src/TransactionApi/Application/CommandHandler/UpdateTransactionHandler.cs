using Marten;
using MediatR;
using Shared;
using TransactionApi.Application.Command;
using TransactionApi.Domain;
using TransactionApi.Domain.ValueObjects;

namespace TransactionApi.Application.CommandHandler;

public class UpdateTransactionHandler(IDocumentStore documentStore) : IRequestHandler<UpdateTransaction>
{
    public async Task Handle(UpdateTransaction command, CancellationToken cancellationToken)
    {
        using var session = documentStore.LightweightSession();

        var aggregate = await session.Events.AggregateStreamAsync<TransactionAggregate>(
            streamId: command.TransactionId,
            token: cancellationToken);

        if (aggregate == null)
        {
            throw new Exception(message: $"Transakcija sa ID-em {command.TransactionId} ne postoji.");
        }

        var transferDetails = command.TransferWallet is not null
            ? new TransferDetails(ToWalletId: command.TransferWallet.ToWalletId,
                ToWalletConversion: new CurrencyConversion(
                    ExchangeRate: command.TransferWallet.ToWalletExchangeRate, FromCurrencyCode: command.CurrencyCode,
                    ToCurrencyCode: command.TransferWallet.ToWalletCurrencyCode
                ))
            : null;

        var events = aggregate.UpdateTransaction(
            transactionId: command.TransactionId,
            newWallet: new WalletDetails(WalletId: command.WalletId,
                DefaultCurrencyConversion: new CurrencyConversion(ExchangeRate: command.DefaultCurrencyExchangeRate,
                    FromCurrencyCode: command.CurrencyCode,
                    ToCurrencyCode: command.DefaultCurrencyCode)),
            newAmount: new Money(value: command.Amount, currencyCode: command.CurrencyCode),
            newCategorization: new TransactionCategorization(
                type: command.TransactionType,
                categoryId: command.TransactionCategoryId),
            newDescription: command.Description,
            userId: command.UserId,
            newOccurredAt: command.OccurredAt,
            newTransferDetails: transferDetails);

        session.Events.Append(stream: command.TransactionId, events: events);

        await session.SaveChangesAsync(token: cancellationToken);
    }
}