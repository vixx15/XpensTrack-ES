using WalletApi.Domain.ValueObjects;

namespace WalletApi.Domain.Events;

public record FundsMoved(
    Guid WalletId,
    string UserId,
    Guid TransactionId,
    Money Amount,
    Money DefaultCurrencyAmount,
    BalanceDirection Direction,
    DateTimeOffset OccurredAt);
