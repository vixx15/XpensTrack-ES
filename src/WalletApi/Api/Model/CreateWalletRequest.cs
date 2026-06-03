namespace WalletApi.Api.Model;

public record CreateWalletRequest(
    string Name,
    decimal Amount,
    string CurrencyCode,
    long WalletTypeId);