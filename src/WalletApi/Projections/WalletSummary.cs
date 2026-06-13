namespace WalletApi.Projections;

public record WalletSummary(
    Guid Id,
    string Name,
    decimal Amount,
    string CurrencyCode,
    int WalletTypeId,
    string TypeName,
    decimal DefaultCurrencyAmount,
    string DefaultCurrencyCode);
