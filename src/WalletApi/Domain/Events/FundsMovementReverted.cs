using WalletApi.Domain.ValueObjects;

namespace WalletApi.Domain.Events;

public record FundsMovementReverted(
    Guid WalletId,
    string UserId,
    Guid TransactionId,
    Money Amount,
    Money DefaultCurrencyAmount,
    BalanceDirection OriginalDirection,
    DateTimeOffset OccurredAt);
