using WalletApi.Domain.ValueObjects;

namespace WalletApi.Domain;

public static class BalanceMath
{
    public static decimal SignedDelta(decimal amount, BalanceDirection direction)
        => direction == BalanceDirection.Added ? amount : -amount;

    public static Money SignedDelta(Money amount, BalanceDirection direction)
        => direction == BalanceDirection.Added ? amount : -amount;
}
