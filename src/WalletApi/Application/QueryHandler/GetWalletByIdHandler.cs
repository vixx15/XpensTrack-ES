using Marten;
using MediatR;
using WalletApi.Domain;
using WalletApi.Infrastructure.Localization;
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
            token: cancellationToken) ?? throw new KeyNotFoundException(message: $"Wallet '{query.Id}' was not found.");

        if (summary.UserId != query.UserId)
            throw new UnauthorizedAccessException(
                $"Wallet '{query.Id}' does not belong to user '{query.UserId}'.");

        return summary with {
            TypeName = displayNames.GetWalletTypeName((WalletType)summary.WalletTypeId)
        };
    }
}