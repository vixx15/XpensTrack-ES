using Shared;

namespace WalletApi.Api.Model;

public record CreateWalletRequest(
    string Name,
    decimal Amount,
    string CurrencyCode,
    WalletType WalletType);
