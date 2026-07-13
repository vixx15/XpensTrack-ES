using WalletApi.Domain;

namespace WalletApi.Api.Model;

public record CreateWalletRequest(
    string Name,
    decimal OpeningBalance,
    string CurrencyCode,
    WalletType WalletType);
