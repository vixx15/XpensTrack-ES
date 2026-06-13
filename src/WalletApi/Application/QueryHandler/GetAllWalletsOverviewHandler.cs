using Marten;
using MediatR;
using Shared;
using WalletApi.Application.Query;
using WalletApi.Projections;

namespace WalletApi.Application.QueryHandler;

public class GetAllWalletsOverviewHandler(IDocumentStore documentStore, WalletTypeDisplayNames displayNames)
    : IRequestHandler<GetAllWalletsOverview, AllWalletsOverview?>
{
    public async Task<AllWalletsOverview?> Handle(
        GetAllWalletsOverview query, CancellationToken ct)
    {
        using var session = documentStore.QuerySession();

        var overview = await session.LoadAsync<AllWalletsOverview>(
            id: query.UserId, token: ct);

        if (overview is null) return null;

        var enrichedCategories = overview.WalletsByCategories
            .Select(category => category with
            {
                Accounts = category.Accounts
                    .Select(account => account with
                    {
                        TypeName = displayNames.GetWalletTypeName((WalletType)account.WalletTypeId)
                    })
                    .ToList()
            })
            .ToList();

        return overview with { WalletsByCategories = enrichedCategories };
    }
}
