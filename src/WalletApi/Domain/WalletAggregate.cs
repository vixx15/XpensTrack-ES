using JasperFx.Core;
using JasperFx.Events;
using Marten.Schema;
using Shared;
using WalletApi.Domain.Events;
using WalletApi.Domain.ValueObjects;

namespace WalletApi.Domain;

public class WalletAggregate
{
    public int Version { get; set; }
    [Identity]
    public Guid WalletId { get; private set; }
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
        Money amount,
        WalletType walletType,
        string userId,
        decimal defaultCurrencyExchangeRate, string defaultCurrencyCode)
    {
        if (!Enum.IsDefined(typeof(WalletType), walletType))
            throw new ArgumentException($"Invalid wallet type value: {(int)walletType}");

        yield return new WalletCreated(
            WalletId: walletId,
            Name: name,
            Amount: amount,
            WalletType: walletType,
            UserId: userId,
            DefaultCurrencyExchangeRate: defaultCurrencyExchangeRate,
            DefaultCurrencyAmount: new Money(value: amount.Value * defaultCurrencyExchangeRate,
                currencyCode: defaultCurrencyCode)
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
        if (!Enum.IsDefined(typeof(WalletType), walletCreated.WalletType))
            throw new ArgumentException($"Invalid wallet type value: {(int)walletCreated.WalletType}");

        WalletId = walletCreated.WalletId;
        Name = walletCreated.Name;
        Amount = new Money(value: walletCreated.Amount.Value, currencyCode: walletCreated.Amount.CurrencyCode);
        WalletType = walletCreated.WalletType;
        UserId = walletCreated.UserId;
        DefaultCurrencyConversion = new CurrencyConversion(
            ExchangeRate: walletCreated.DefaultCurrencyExchangeRate,
            FromCurrencyCode: walletCreated.Amount.CurrencyCode,
            ToCurrencyCode: walletCreated.DefaultCurrencyAmount.CurrencyCode
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
}
