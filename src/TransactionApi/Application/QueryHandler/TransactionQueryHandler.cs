using Marten;
using MediatR;
using Shared;
using TransactionApi.Application.Query;
using TransactionApi.Projections;

namespace TransactionApi.Application.QueryHandler;

public class TransactionQueryHandler(IQuerySession session, TransactionDisplayNames displayNames) :
    IRequestHandler<GetTransactionById, TransactionReadModel?>,
    IRequestHandler<GetUserTransactions, IReadOnlyList<TransactionReadModel>>
{
    public async Task<TransactionReadModel?> Handle(GetTransactionById query, CancellationToken ct)
    {
        var model = await session.LoadAsync<TransactionReadModel>(id: query.Id, token: ct);
        if (model is null) return null;

        return Enrich(model);
    }

    public async Task<IReadOnlyList<TransactionReadModel>> Handle(GetUserTransactions query, CancellationToken ct)
    {
        var models = await session.Query<TransactionReadModel>()
            .Where(predicate: x => x.UserId == query.UserId)
            .OrderByDescending(keySelector: x => x.Time)
            .ToListAsync(token: ct);

        return models.Select(Enrich).ToList();
    }

    private TransactionReadModel Enrich(TransactionReadModel model)
    {
        return model with
        {
            CategoryName = model.CategoryId.HasValue
                ? displayNames.GetCategoryName((TransactionCategory)model.CategoryId.Value)
                : null,
            TypeName = displayNames.GetTypeName(model.TransactionType)
        };
    }
}