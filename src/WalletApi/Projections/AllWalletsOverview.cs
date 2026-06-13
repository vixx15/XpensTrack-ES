using System.ComponentModel.DataAnnotations;
using Marten.Schema;

namespace WalletApi.Projections;

public record AllWalletsOverview(
    [property: Identity] string UserId,
    List<WalletsByCategoryStats> WalletsByCategories,
    string CurrencyCode,
    decimal Total = 0.0M,
    decimal Assets = 0.0M,
    decimal Liabilities = 0.0M
);

public record WalletsByCategoryStats(
    int CategoryId,
    List<WalletSummary> Accounts,
    decimal Total = 0.0M,
    decimal Assets = 0.0M,
    decimal Liabilities = 0.0M);
