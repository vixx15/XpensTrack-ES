using Marten;
using MediatR;
using Shared.Contracts;
using TransactionApi.Application.Command;
using TransactionApi.Domain;
using TransactionApi.Domain.Events;
using Shared.Outbox;

namespace TransactionApi.Application.CommandHandler;

public class DeleteTransactionHandler(
    IDocumentStore documentStore) : IRequestHandler<DeleteTransaction>
{
    public async Task Handle(DeleteTransaction command, CancellationToken cancellationToken)
    {
        using var session = documentStore.LightweightSession();

        var stream = await session.Events.FetchForWriting<TransactionAggregate>(
            command.TransactionId, cancellationToken);

        var aggregate = stream.Aggregate
            ?? throw new Exception(message: $"Transakcija sa ID-em {command.TransactionId} ne postoji.");

        var events = aggregate.DeleteTransaction(transactionId: command.TransactionId, userId: command.UserId);
        var eventList = events.ToList();
        stream.AppendMany(eventList);
        session.Events.ArchiveStream(command.TransactionId);

        var deletedEvent = eventList.OfType<TransactionApi.Domain.Events.TransactionDeleted>().FirstOrDefault();
        if (deletedEvent is not null)
        {
            session.Store(OutboxMessage.From(new TransactionDeletedIntegrationEvent(
                TransactionId: deletedEvent.TransactionId,
                WalletId: deletedEvent.WalletId,
                UserId: deletedEvent.UserId,
                Amount: deletedEvent.Amount.Value,
                AmountCurrencyCode: deletedEvent.Amount.CurrencyCode,
                TransactionType: deletedEvent.TransactionType.ToString(),
                OccuredAt: deletedEvent.OccuredAt,
                DefaultCurrencyAmount: deletedEvent.DefaultCurrencyAmount.Value,
                DefaultCurrencyCode: deletedEvent.DefaultCurrencyAmount.CurrencyCode,
                ToWalletId: deletedEvent.ToWalletId,
                ToWalletAmount: deletedEvent.ToWalletAmount?.Value,
                ToWalletAmountCurrencyCode: deletedEvent.ToWalletAmount?.CurrencyCode,
                ToWalletCurrencyExchangeRate: deletedEvent.ToWalletCurrencyExchangeRate,
                ToWalletCurrencyCode: deletedEvent.ToWalletCurrencyCode)));
        }

        await session.SaveChangesAsync(token: cancellationToken);
    }
}