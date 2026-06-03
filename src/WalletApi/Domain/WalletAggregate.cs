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
    public long WalletTypeId { get; private set; }
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
        long walletTypeId,
        string userId,
        decimal defaultCurrencyExchangeRate, string defaultCurrencyCode)
    {
        yield return new WalletCreated(
            WalletId: walletId,
            Name: name,
            Amount: amount,
            WalletTypeId: walletTypeId,
            UserId: userId,
            DefaultCurrencyExchangeRate: defaultCurrencyExchangeRate,
            DefaultCurrencyAmount: new Money(value: amount.Value * defaultCurrencyExchangeRate,
                currencyCode: defaultCurrencyCode)
        );
    }

    public IEnumerable<object> UpdateWallet(Guid walletId,
        string userId,
        string name, long walletTypeId)
    {
        if (name != Name && name.IsNotEmpty())
        {
            yield return new WalletNameChanged(WalletId: walletId, userId, NewName: name);
        }

        if (walletTypeId != WalletTypeId && walletTypeId > 0)
        {
            yield return new WalletTypeChanged(WalletId: walletId, userId, NewTypeId: walletTypeId,
                WalletTypeId: WalletTypeId);
        }
    }

    public WalletAggregate(WalletCreated walletCreated)
    {
        WalletId = walletCreated.WalletId;
        Name = walletCreated.Name;
        Amount = new Money(value: walletCreated.Amount.Value, currencyCode: walletCreated.Amount.CurrencyCode);
        WalletTypeId = walletCreated.WalletTypeId;
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
        WalletTypeId = @event.Data.NewTypeId;
    }
}