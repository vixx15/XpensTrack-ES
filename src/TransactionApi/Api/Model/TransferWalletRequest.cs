namespace TransactionApi.Api.Model;

public record TransferWalletRequest(
    Guid ToWalletId,
    string ToWalletCurrencyCode
);