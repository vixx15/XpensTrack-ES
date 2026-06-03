using Marten;
using MediatR;
using WalletApi.Application.Query;
using WalletApi.Projections;

namespace WalletApi.Application.QueryHandler;

public class GetWalletByIdHandler(IDocumentStore documentStore) : IRequestHandler<GetWalletById, WalletSummary?>
{
    public async Task<WalletSummary?> Handle(GetWalletById query, CancellationToken cancellationToken)
    {
        using var session = documentStore.QuerySession();

        return await session.LoadAsync<WalletSummary>(
            id: query.Id,
            token: cancellationToken);
    }
}