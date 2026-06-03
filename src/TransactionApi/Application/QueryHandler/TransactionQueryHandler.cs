using Marten;
using MediatR;
using TransactionApi.Application.Query;
using TransactionApi.Projections;

namespace TransactionApi.Application.QueryHandler;

public class TransactionQueryHandler(IQuerySession session) :
    IRequestHandler<GetTransactionById, TransactionReadModel?>,
    IRequestHandler<GetUserTransactions, IReadOnlyList<TransactionReadModel>>
{
    public async Task<TransactionReadModel?> Handle(GetTransactionById query, CancellationToken ct)
    {
        return await session.LoadAsync<TransactionReadModel>(id: query.Id, token: ct);
    }

    public async Task<IReadOnlyList<TransactionReadModel>> Handle(GetUserTransactions query, CancellationToken ct)
    {
        return await session.Query<TransactionReadModel>()
            .Where(predicate: x => x.UserId == query.UserId)
            .OrderByDescending(keySelector: x => x.Time)
            .ToListAsync(token: ct);
    }
}