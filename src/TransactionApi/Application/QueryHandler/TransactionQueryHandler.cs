using Marten;
using MediatR;
using TransactionApi.Application.Query;
using TransactionApi.Domain;
using TransactionApi.Infrastructure.Documents;
using TransactionApi.Infrastructure.Localization;
using TransactionApi.Projections;

namespace TransactionApi.Application.QueryHandler;

public class TransactionQueryHandler(IQuerySession session, TransactionDisplayNames displayNames) :
    IRequestHandler<GetTransactionById, TransactionReadModel?>,
    IRequestHandler<GetUserTransactions, IReadOnlyList<TransactionReadModel>>
{
    public async Task<TransactionReadModel?> Handle(GetTransactionById query, CancellationToken ct)
    {
        var model = await session.LoadAsync<TransactionReadModel>(id: query.Id, token: ct)
            ?? throw new KeyNotFoundException($"Transaction '{query.Id}' was not found.");

        if (model.UserId != query.UserId)
            throw new UnauthorizedAccessException(
                $"Transaction '{query.Id}' does not belong to user '{query.UserId}'.");

        var walletIds = WalletIds([model]);
        var walletNames = await LoadWalletNames(walletIds, ct);
        return Enrich(model, walletNames);
    }

    public async Task<IReadOnlyList<TransactionReadModel>> Handle(GetUserTransactions query, CancellationToken ct)
    {
        var models = await session.Query<TransactionReadModel>()
            .Where(predicate: x => x.UserId == query.UserId)
            .OrderByDescending(keySelector: x => x.Time)
            .ToListAsync(token: ct);

        var walletIds = WalletIds(models);
        var walletNames = await LoadWalletNames(walletIds, ct);
        return models.Select(m => Enrich(m, walletNames)).ToList();
    }

    private static IEnumerable<Guid> WalletIds(IEnumerable<TransactionReadModel> models) =>
        models.SelectMany(m => new[] { (Guid?)m.WalletId, m.ToWalletId })
              .Where(id => id.HasValue)
              .Select(id => id!.Value)
              .Distinct();

    private async Task<Dictionary<Guid, string>> LoadWalletNames(IEnumerable<Guid> walletIds, CancellationToken ct)
    {
        var ids = walletIds.ToArray();
        if (ids.Length == 0) return [];

        var refs = await session.LoadManyAsync<WalletReference>(ct, ids);
        return refs.Where(r => r is not null)
                   .ToDictionary(r => r!.Id, r => r!.Name);
    }

    private TransactionReadModel Enrich(TransactionReadModel model, Dictionary<Guid, string> walletNames)
    {
        return model with
        {
            CategoryName = model.CategoryId.HasValue
                ? displayNames.GetCategoryName((TransactionCategory)model.CategoryId.Value)
                : null,
            TypeName = displayNames.GetTypeName(model.TransactionType),
            WalletName = walletNames.TryGetValue(model.WalletId, out var name) ? name : null,
            ToWalletName = model.ToWalletId.HasValue && walletNames.TryGetValue(model.ToWalletId.Value, out var toName) ? toName : null
        };
    }
}