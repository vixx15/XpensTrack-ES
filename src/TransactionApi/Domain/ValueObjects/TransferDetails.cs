namespace TransactionApi.Domain.ValueObjects;

public sealed record TransferDetails(
    Guid ToWalletId,
    CurrencyConversion ToWalletConversion)
{
    public static TransferDetails Create(
        Guid toWalletId,
        CurrencyConversion toWalletConversion)
    {
        if (toWalletId == Guid.Empty)
        {
            throw new ArgumentException("Target wallet is required.");
        }

        return new TransferDetails(
            ToWalletId: toWalletId,
            ToWalletConversion: toWalletConversion);
    }

    public Money ConvertToTargetWallet(Money sourceAmount)
    {
        if (ToWalletId == Guid.Empty)
        {
            throw new ArgumentException("Target wallet id is required.");
        }

        return ToWalletConversion.Convert(money: sourceAmount);
    }
}