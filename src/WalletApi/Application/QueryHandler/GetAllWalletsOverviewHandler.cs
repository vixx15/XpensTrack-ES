using Marten;
using MediatR;
using WalletApi.Application.Query;
using WalletApi.Projections;

namespace WalletApi.Application.QueryHandler;

public class GetAllWalletsOverviewHandler(IDocumentStore documentStore)
    : IRequestHandler<GetAllWalletsOverview, AllWalletsOverview?>
{
    public async Task<AllWalletsOverview?> Handle(
        GetAllWalletsOverview query, CancellationToken ct)
    {
        using var session = documentStore.QuerySession();

        return await session.LoadAsync<AllWalletsOverview>(
            id: query.UserId, token: ct);
    }
}
