using Marten;
using MediatR;
using Shared;
using WalletApi.Application.Query;
using WalletApi.Projections;

namespace WalletApi.Application.QueryHandler;

public class GetWalletByIdHandler(IDocumentStore documentStore, WalletTypeDisplayNames displayNames)
    : IRequestHandler<GetWalletById, WalletSummary?>
{
    public async Task<WalletSummary?> Handle(GetWalletById query, CancellationToken cancellationToken)
    {
        using var session = documentStore.QuerySession();

        var summary = await session.LoadAsync<WalletSummary>(
            id: query.Id,
            token: cancellationToken);

        if (summary is null) return null;

        return summary with
        {
            TypeName = displayNames.GetWalletTypeName((WalletType)summary.WalletTypeId)
        };
    }
}
