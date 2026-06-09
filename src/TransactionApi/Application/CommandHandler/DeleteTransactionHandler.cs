using Marten;
using MediatR;
using TransactionApi.Application.Command;
using TransactionApi.Domain;

namespace TransactionApi.Application.CommandHandler;

public class DeleteTransactionHandler(IDocumentStore documentStore) : IRequestHandler<DeleteTransaction>
{
    public async Task Handle(DeleteTransaction command, CancellationToken cancellationToken)
    {
        using var session = documentStore.LightweightSession();

        var aggregate = await session.Events.AggregateStreamAsync<TransactionAggregate>(
            streamId: command.TransactionId,
            token: cancellationToken);

        if (aggregate == null)
        {
            throw new Exception(message: $"Transakcija sa ID-em {command.TransactionId} ne postoji.");
        }

        var events = aggregate.DeleteTransaction(transactionId: command.TransactionId, userId: command.UserId);
        session.Events.Append(stream: command.TransactionId, events: events);
        session.Events.ArchiveStream(command.TransactionId); 
        await session.SaveChangesAsync();
    }
}