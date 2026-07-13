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

public class UpdateTransactionHandler(
    IDocumentStore documentStore,
    IExchangeRateProvider exchangeRateProvider)
    : IRequestHandler<UpdateTransaction>
{
    public async Task Handle(UpdateTransaction command, CancellationToken cancellationToken)
    {
        using var session = documentStore.LightweightSession();

        var stream = await session.Events.FetchForWriting<TransactionAggregate>(
            command.TransactionId, cancellationToken);

        var aggregate = stream.Aggregate
            ?? throw new Exception(message: $"Transakcija sa ID-em {command.TransactionId} ne postoji.");

        if (aggregate.UserId != command.UserId)
            throw new UnauthorizedAccessException($"Transaction '{command.TransactionId}' does not belong to user '{command.UserId}'.");

        var wallet = await session.LoadAsync<WalletReference>(command.WalletId, cancellationToken)
            ?? throw new KeyNotFoundException($"Wallet '{command.WalletId}' not found.");

        if (wallet.UserId != command.UserId)
            throw new UnauthorizedAccessException($"Wallet '{command.WalletId}' does not belong to user '{command.UserId}'.");

        if (!string.Equals(wallet.CurrencyCode, command.CurrencyCode, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Transaction currency '{command.CurrencyCode}' does not match wallet currency '{wallet.CurrencyCode}'.");

        if (command.TransferWallet is not null)
        {
            var toWallet = await session.LoadAsync<WalletReference>(command.TransferWallet.ToWalletId, cancellationToken)
                ?? throw new KeyNotFoundException($"Target wallet '{command.TransferWallet.ToWalletId}' not found.");

            if (toWallet.UserId != command.UserId)
                throw new UnauthorizedAccessException($"Target wallet '{command.TransferWallet.ToWalletId}' does not belong to user '{command.UserId}'.");
        }

        var defaultCurrencyExchangeRate =
            await exchangeRateProvider.GetRateAsync(command.CurrencyCode, command.DefaultCurrencyCode,
                command.OccurredAt, cancellationToken);

        TransferDetails? transferDetails = null;
        if (command.TransferWallet is not null)
        {
            var toWalletRate = await exchangeRateProvider.GetRateAsync(command.CurrencyCode,
                command.TransferWallet.ToWalletCurrencyCode, command.OccurredAt, cancellationToken);
            transferDetails = new TransferDetails(
                ToWalletId: command.TransferWallet.ToWalletId,
                ToWalletConversion: new CurrencyConversion(
                    ExchangeRate: toWalletRate,
                    FromCurrencyCode: command.CurrencyCode,
                    ToCurrencyCode: command.TransferWallet.ToWalletCurrencyCode));
        }

        var events = aggregate.UpdateTransaction(
            transactionId: command.TransactionId,
            newWallet: new WalletDetails(WalletId: command.WalletId,
                DefaultCurrencyConversion: new CurrencyConversion(
                    ExchangeRate: defaultCurrencyExchangeRate,
                    FromCurrencyCode: command.CurrencyCode,
                    ToCurrencyCode: command.DefaultCurrencyCode)),
            newAmount: new Money(value: command.Amount, currencyCode: command.CurrencyCode),
            newCategorization: new TransactionCategorization(
                type: command.TransactionType,
                category: command.Category),
            newDescription: command.Description,
            userId: command.UserId,
            newOccurredAt: command.OccurredAt,
            newTransferDetails: transferDetails);

        var eventList = events.ToList();
        stream.AppendMany(eventList);

        var updatedEvent = eventList.OfType<TransactionApi.Domain.Events.TransactionUpdated>().FirstOrDefault();
        if (updatedEvent is not null)
        {
            session.Store(OutboxMessage.From(new TransactionUpdatedIntegrationEvent(
                TransactionId: updatedEvent.TransactionId,
                OldWalletId: updatedEvent.OldWalletId,
                UserId: updatedEvent.UserId,
                OldAmount: updatedEvent.OldAmount.Value,
                OldAmountCurrencyCode: updatedEvent.OldAmount.CurrencyCode,
                OldDefaultCurrencyAmount: updatedEvent.OldDefaultCurrencyAmount.Value,
                OldDefaultCurrencyCode: updatedEvent.OldDefaultCurrencyAmount.CurrencyCode,
                OldTransactionType: updatedEvent.OldTransactionType.ToString(),
                OldTransactionCategory: updatedEvent.OldTransactionCategory?.ToString(),
                OldDescription: updatedEvent.OldDescription,
                OldOccuredAt: updatedEvent.OldOccuredAt,
                OldToWalletId: updatedEvent.OldToWalletId,
                OldToWalletAmount: updatedEvent.OldToWalletAmount?.Value,
                OldToWalletAmountCurrencyCode: updatedEvent.OldToWalletAmount?.CurrencyCode,
                OldToWalletCurrencyExchangeRate: updatedEvent.OldToWalletCurrencyExchangeRate,
                OldToWalletCurrencyCode: updatedEvent.OldToWalletCurrencyCode,
                NewWalletId: updatedEvent.NewWalletId,
                NewWalletExchangeRate: updatedEvent.NewWalletExchangeRate,
                NewWalletCurrencyCode: updatedEvent.NewWalletCurrencyCode,
                NewAmount: updatedEvent.NewAmount.Value,
                NewAmountCurrencyCode: updatedEvent.NewAmount.CurrencyCode,
                NewDefaultCurrencyAmount: updatedEvent.NewDefaultCurrencyAmount.Value,
                NewDefaultCurrencyCode: updatedEvent.NewDefaultCurrencyAmount.CurrencyCode,
                NewTransactionType: updatedEvent.NewTransactionType.ToString(),
                NewTransactionCategory: updatedEvent.NewTransactionCategory?.ToString(),
                NewDescription: updatedEvent.NewDescription,
                NewOccurredAt: updatedEvent.NewOccurredAt,
                NewToWalletId: updatedEvent.NewToWalletId,
                NewToWalletAmount: updatedEvent.NewToWalletAmount?.Value,
                NewToWalletAmountCurrencyCode: updatedEvent.NewToWalletAmount?.CurrencyCode,
                NewToWalletCurrencyExchangeRate: updatedEvent.NewToWalletCurrencyExchangeRate,
                NewToWalletCurrencyCode: updatedEvent.NewToWalletCurrencyCode)));
        }

        await session.SaveChangesAsync(token: cancellationToken);
    }
}