namespace Shared.Contracts;

public record TransactionCreatedIntegrationEvent(
    Guid TransactionId,
    Guid WalletId,
    string UserId,
    decimal Amount,
    string AmountCurrencyCode,
    string TransactionType,
    DateTimeOffset OccuredAt,
    decimal DefaultCurrencyAmount,
    string DefaultCurrencyCode,
    Guid? ToWalletId,
    decimal? ToWalletAmount,
    string? ToWalletAmountCurrencyCode,
    decimal? ToWalletCurrencyExchangeRate,
    string? ToWalletCurrencyCode);