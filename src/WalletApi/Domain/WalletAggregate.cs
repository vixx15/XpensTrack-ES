using JasperFx.Core;
using JasperFx.Events;
using Marten.Schema;
using WalletApi.Domain.Events;
using WalletApi.Domain.ValueObjects;

namespace WalletApi.Domain;

public class WalletAggregate
{
    public int Version { get; set; }
    [Identity] public Guid WalletId { get; private set; }
    public string Name { get; private set; }
    public Money Amount { get; private set; }
    public WalletType WalletType { get; private set; }
    public string UserId { get; private set; }
    public CurrencyConversion DefaultCurrencyConversion { get; private set; }

    public Money GetDefaultCurrencyMoney()
    {
        return DefaultCurrencyConversion.Convert(money: Amount);
    }

    public static IEnumerable<object> Create(
        Guid walletId,
        string name,
        Money openingBalance,
        WalletType walletType,
        string userId,
        decimal defaultCurrencyExchangeRate, string defaultCurrencyCode)
    {
        if (!Enum.IsDefined(typeof(WalletType), walletType))
            throw new ArgumentException($"Invalid wallet type value: {(int)walletType}");

        if (openingBalance.Value < 0 && walletType is not (WalletType.CreditCard or WalletType.Investment))
            throw new ArgumentException(
                $"Opening balance cannot be negative for wallet type '{walletType}'.", paramName: nameof(openingBalance));

        var defaultCurrencyConversion = new CurrencyConversion(
            ExchangeRate: defaultCurrencyExchangeRate,
            FromCurrencyCode: openingBalance.CurrencyCode,
            ToCurrencyCode: defaultCurrencyCode);

        yield return new WalletCreated(
            WalletId: walletId,
            Name: name,
            OpeningBalance: openingBalance,
            WalletType: walletType,
            UserId: userId,
            DefaultCurrencyExchangeRate: defaultCurrencyExchangeRate,
            DefaultCurrencyOpeningBalance: defaultCurrencyConversion.Convert(openingBalance)
        );
    }

    public IEnumerable<object> UpdateWallet(Guid walletId,
        string userId,
        string name, WalletType walletType)
    {
        if (!Enum.IsDefined(typeof(WalletType), walletType))
            throw new ArgumentException($"Invalid wallet type value: {(int)walletType}");

        if (name != Name && name.IsNotEmpty())
        {
            yield return new WalletNameChanged(WalletId: walletId, userId, NewName: name);
        }

        if (walletType != WalletType)
        {
            yield return new WalletTypeChanged(WalletId: walletId, userId, NewType: walletType,
                OldType: WalletType);
        }
    }

    public WalletAggregate(WalletCreated walletCreated)
    {
        WalletId = walletCreated.WalletId;
        Name = walletCreated.Name;
        Amount = new Money(value: walletCreated.OpeningBalance.Value, currencyCode: walletCreated.OpeningBalance.CurrencyCode);
        WalletType = walletCreated.WalletType;
        UserId = walletCreated.UserId;
        DefaultCurrencyConversion = new CurrencyConversion(
            ExchangeRate: walletCreated.DefaultCurrencyExchangeRate,
            FromCurrencyCode: walletCreated.OpeningBalance.CurrencyCode,
            ToCurrencyCode: walletCreated.DefaultCurrencyOpeningBalance.CurrencyCode
        );
    }

    public void Apply(IEvent<WalletNameChanged> @event)
    {
        Name = @event.Data.NewName;
    }

    public void Apply(IEvent<WalletTypeChanged> @event)
    {
        WalletType = @event.Data.NewType;
    }

    public IEnumerable<object> ApplyTransaction(
        string userId,
        Guid transactionId,
        Money amount,
        Money defaultCurrencyAmount,
        BalanceDirection direction,
        DateTimeOffset occurredAt)
    {
        EnsureOwnedBy(userId);
        EnsureCurrencyMatches(amount);

        yield return new Events.FundsMoved(
            WalletId: WalletId,
            UserId: userId,
            TransactionId: transactionId,
            Amount: amount,
            DefaultCurrencyAmount: defaultCurrencyAmount,
            Direction: direction,
            OccurredAt: occurredAt);
    }

    public IEnumerable<object> RevertTransaction(
        string userId,
        Guid transactionId,
        Money amount,
        Money defaultCurrencyAmount,
        BalanceDirection originalDirection,
        DateTimeOffset occurredAt)
    {
        EnsureOwnedBy(userId);
        EnsureCurrencyMatches(amount);

        yield return new Events.FundsMovementReverted(
            WalletId: WalletId,
            UserId: userId,
            TransactionId: transactionId,
            Amount: amount,
            DefaultCurrencyAmount: defaultCurrencyAmount,
            OriginalDirection: originalDirection,
            OccurredAt: occurredAt);
    }

    public IEnumerable<object> AdjustTransaction(
        string userId,
        Guid transactionId,
        Money oldAmount,
        Money oldDefaultCurrencyAmount,
        BalanceDirection oldDirection,
        Money newAmount,
        Money newDefaultCurrencyAmount,
        BalanceDirection newDirection,
        DateTimeOffset occurredAt)
    {
        EnsureOwnedBy(userId);
        EnsureCurrencyMatches(oldAmount);
        EnsureCurrencyMatches(newAmount);

        yield return new Events.FundsMovementAdjusted(
            WalletId: WalletId,
            UserId: userId,
            TransactionId: transactionId,
            OldAmount: oldAmount,
            OldDefaultCurrencyAmount: oldDefaultCurrencyAmount,
            OldDirection: oldDirection,
            NewAmount: newAmount,
            NewDefaultCurrencyAmount: newDefaultCurrencyAmount,
            NewDirection: newDirection,
            OccurredAt: occurredAt);
    }

    public void Apply(IEvent<Events.FundsMoved> @event)
    {
        Amount += BalanceMath.SignedDelta(@event.Data.Amount, @event.Data.Direction);
    }

    public void Apply(IEvent<Events.FundsMovementReverted> @event)
    {
        Amount -= BalanceMath.SignedDelta(@event.Data.Amount, @event.Data.OriginalDirection);
    }

    public void Apply(IEvent<Events.FundsMovementAdjusted> @event)
    {
        Amount += -BalanceMath.SignedDelta(@event.Data.OldAmount, @event.Data.OldDirection)
                  + BalanceMath.SignedDelta(@event.Data.NewAmount, @event.Data.NewDirection);
    }
    
    private void EnsureOwnedBy(string userId)
    {
        if (userId != UserId)
        {
            throw new InvalidOperationException(
                $"Wallet '{WalletId}' does not belong to user '{userId}'.");
        }
    }

    private void EnsureCurrencyMatches(Money amount)
    {
        if (amount.CurrencyCode != Amount.CurrencyCode)
        {
            throw new InvalidOperationException(
                $"Currency mismatch on wallet '{WalletId}': wallet is {Amount.CurrencyCode}, amount is {amount.CurrencyCode}.");
        }
    }
}