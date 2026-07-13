using WalletApi.Domain.ValueObjects;

namespace WalletApi.Domain.Events;

public record FundsMovementAdjusted(
    Guid WalletId,
    string UserId,
    Guid TransactionId,
    Money OldAmount,
    Money OldDefaultCurrencyAmount,
    BalanceDirection OldDirection,
    Money NewAmount,
    Money NewDefaultCurrencyAmount,
    BalanceDirection NewDirection,
    DateTimeOffset OccurredAt);
