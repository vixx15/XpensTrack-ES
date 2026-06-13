using JasperFx.Events;
using Marten.Metadata;
using Marten.Schema;
using Shared;
using TransactionApi.Domain.Events;
using TransactionApi.Domain.ValueObjects;

namespace TransactionApi.Domain;

public class TransactionAggregate
{
    public int Version { get; set; }
    [Identity] public Guid TransactionId { get; private set; }
    public WalletDetails Wallet { get; private set; }
    public Money Money { get; private set; }
    public TransactionCategorization Categorization { get; private set; }
    public string Description { get; private set; }
    public DateTimeOffset OccuredAt { get; private set; }
    public TransferDetails? TransferDetails { get; private set; }

    public Money GetDefaultCurrencyMoney()
    {
        return Wallet.DefaultCurrencyConversion.Convert(money: Money);
    }

    public bool Deleted { get; private set; }

    public static IEnumerable<object> Create(
        Guid transactionId,
        string userId,
        Guid walletId,
        Money amount,
        TransactionCategorization categorization,
        string description,
        DateTimeOffset occuredAt,
        string defaultCurrencyCode,
        decimal defaultCurrencyExchangeRate,
        TransferDetails? transferDetails)
    {
        ValidateTransactionState(
            walletId: walletId,
            amount: amount,
            categorization: categorization,
            transferDetails: transferDetails);

        Money? toWalletMoney = null;
        if (transferDetails != null)
        {
            toWalletMoney = new Money(value: amount.Value * transferDetails.ToWalletConversion.ExchangeRate,
                currencyCode: transferDetails.ToWalletConversion.ToCurrencyCode);
        }

        yield return new TransactionCreated(
            TransactionId: transactionId,
            WalletId: walletId,
            UserId: userId,
            Amount: amount,
            TransactionType: categorization.Type,
            TransactionCategory: categorization.Category,
            Description: description,
            OccuredAt: occuredAt,
            DefaultCurrencyAmount: new Money(value: amount.Value * defaultCurrencyExchangeRate,
                currencyCode: defaultCurrencyCode),
            DefaultCurrencyExchangeRate: defaultCurrencyExchangeRate,
            ToWalletId: transferDetails?.ToWalletId,
            ToWalletAmount: toWalletMoney,
            ToWalletCurrencyExchangeRate: transferDetails?.ToWalletConversion.ExchangeRate,
            ToWalletCurrencyCode: transferDetails?.ToWalletConversion.ToCurrencyCode);
    }

    public TransactionAggregate(TransactionCreated created)
    {
        var transactionCurrencyCode = created.Amount.CurrencyCode;

        Wallet = new WalletDetails(WalletId: created.WalletId, DefaultCurrencyConversion: new CurrencyConversion(
            ExchangeRate: created.DefaultCurrencyExchangeRate,
            FromCurrencyCode: transactionCurrencyCode,
            ToCurrencyCode: created.DefaultCurrencyAmount.CurrencyCode));

        TransactionId = created.TransactionId;
        Categorization =
            new TransactionCategorization(type: created.TransactionType, category: created.TransactionCategory);
        Description = created.Description;
        OccuredAt = created.OccuredAt;

        if (created.TransactionType == TransactionType.Transfer)
        {
            TransferDetails =
                CreateTransferDetailsFrom(created: created, transactionCurrencyCode: transactionCurrencyCode);
        }
        else
        {
            TransferDetails = null;
        }

        Money = created.Amount;
    }

    public IEnumerable<object> UpdateTransaction(
        Guid transactionId,
        WalletDetails newWallet,
        Money newAmount,
        TransactionCategorization newCategorization,
        string newDescription,
        string userId,
        DateTimeOffset newOccurredAt,
        TransferDetails? newTransferDetails
    )
    {
        if (Deleted)
        {
            throw new InvalidOperationException("Nije moguće modifikovati obrisanu transakciju.");
        }

        ValidateTransactionState(
            walletId: newWallet.WalletId,
            amount: newAmount,
            categorization: newCategorization,
            transferDetails: newTransferDetails);


        var hasChanges = newWallet != Wallet ||
                         newAmount != Money ||
                         newCategorization != Categorization ||
                         newDescription != Description ||
                         newOccurredAt != OccuredAt ||
                         newTransferDetails != TransferDetails;

        if (!hasChanges)
        {
            yield break;
        }

        var newDefaultCurrencyAmount = newWallet.DefaultCurrencyConversion.Convert(money: newAmount);

        Money? toWalletMoney = null;
        if (newTransferDetails != null)
        {
            toWalletMoney = new Money(
                value: newAmount.Value * newTransferDetails.ToWalletConversion.ExchangeRate,
                currencyCode: newTransferDetails.ToWalletConversion.ToCurrencyCode);
        }

        yield return new TransactionUpdated(
            TransactionId: transactionId,
            UserId: userId,
            OldWalletId: Wallet.WalletId,
            OldAmount: Money,
            OldDefaultCurrencyAmount: GetDefaultCurrencyMoney(),
            OldTransactionType: Categorization.Type,
            OldTransactionCategory: Categorization.Category,
            OldDescription: Description,
            OldOccuredAt: OccuredAt,
            OldToWalletId: TransferDetails?.ToWalletId,
            NewWalletId: newWallet.WalletId,
            NewWalletExchangeRate: newWallet.DefaultCurrencyConversion.ExchangeRate,
            NewWalletCurrencyCode: newWallet.DefaultCurrencyConversion.FromCurrencyCode,
            NewAmount: newAmount,
            NewDefaultCurrencyAmount: newDefaultCurrencyAmount,
            NewTransactionType: newCategorization.Type,
            NewTransactionCategory: newCategorization.Category,
            NewDescription: newDescription,
            NewOccurredAt: newOccurredAt,
            NewToWalletId: newTransferDetails?.ToWalletId,
            NewToWalletAmount: toWalletMoney,
            NewToWalletCurrencyExchangeRate: newTransferDetails?.ToWalletConversion.ExchangeRate,
            NewToWalletCurrencyCode: newTransferDetails?.ToWalletConversion.ToCurrencyCode
        );
    }

    public IEnumerable<object> DeleteTransaction(Guid transactionId, string userId)
    {
        if (Deleted)
        {
            throw new InvalidOperationException("Transakcija je vec obrisana.");
        }

        yield return new TransactionDeleted(
            TransactionId: transactionId,
            WalletId: Wallet.WalletId,
            Amount: Money,
            UserId: userId,
            TransactionType: Categorization.Type,
            TransactionCategory: Categorization.Category,
            OccuredAt: OccuredAt,
            DefaultCurrencyAmount: GetDefaultCurrencyMoney(),
            ToWalletId: TransferDetails?.ToWalletId,
            ToWalletAmount: TransferDetails?.ConvertToTargetWallet(Money),
            ToWalletCurrencyExchangeRate: TransferDetails?.ToWalletConversion.ExchangeRate,
            ToWalletCurrencyCode: TransferDetails?.ToWalletConversion.ToCurrencyCode
        );
    }

    public void Apply(IEvent<TransactionDeleted> @event)
    {
        Deleted = true;
    }

    public void Apply(IEvent<TransactionUpdated> @event)
    {
        var e = @event.Data;

        TransactionId = e.TransactionId;
        Description = e.NewDescription;
        OccuredAt = e.NewOccurredAt;
        Money = e.NewAmount;

        Categorization =
            new TransactionCategorization(type: e.NewTransactionType, category: e.NewTransactionCategory);

        Wallet = new WalletDetails(
            WalletId: e.NewWalletId,
            DefaultCurrencyConversion: new CurrencyConversion(
                ExchangeRate: e.NewWalletExchangeRate,
                FromCurrencyCode: e.NewWalletCurrencyCode,
                ToCurrencyCode: Wallet.DefaultCurrencyConversion.ToCurrencyCode)
        );

        if (e.NewTransactionType == TransactionType.Transfer)
        {
            if (e.NewToWalletId is null || e.NewToWalletCurrencyExchangeRate is null ||
                string.IsNullOrWhiteSpace(value: e.NewToWalletCurrencyCode))
            {
                throw new InvalidOperationException("Transfer target data is incomplete in event.");
            }

            TransferDetails = new TransferDetails(
                ToWalletId: e.NewToWalletId.Value,
                ToWalletConversion: new CurrencyConversion(
                    ExchangeRate: e.NewToWalletCurrencyExchangeRate.Value,
                    FromCurrencyCode: e.NewWalletCurrencyCode,
                    ToCurrencyCode: e.NewToWalletCurrencyCode));
        }
        else
        {
            TransferDetails = null;
        }
    }

    private static TransferDetails CreateTransferDetailsFrom(
        TransactionCreated created,
        string transactionCurrencyCode)
    {
        if (created.ToWalletId is null)
        {
            throw new InvalidOperationException("Transfer event is missing target wallet.");
        }

        if (created.ToWalletCurrencyExchangeRate is null)
        {
            throw new InvalidOperationException("Transfer event is missing target wallet exchange rate.");
        }

        if (string.IsNullOrWhiteSpace(value: created.ToWalletCurrencyCode))
        {
            throw new InvalidOperationException("Transfer event is missing target wallet currency code.");
        }

        return new TransferDetails(
            ToWalletId: created.ToWalletId.Value,
            ToWalletConversion: new CurrencyConversion(
                ExchangeRate: created.ToWalletCurrencyExchangeRate.Value,
                FromCurrencyCode: transactionCurrencyCode,
                ToCurrencyCode: created.ToWalletCurrencyCode));
    }

    private static void ValidateTransactionState(
        Guid walletId,
        Money amount,
        TransactionCategorization categorization,
        TransferDetails? transferDetails)
    {
        if (walletId == Guid.Empty)
        {
            throw new ArgumentException("Wallet is required.", paramName: nameof(walletId));
        }

        if (amount.Value <= 0)
        {
            throw new ArgumentException("Transaction amount must be greater than zero.", paramName: nameof(amount));
        }

        if (categorization.Type == TransactionType.Transfer && transferDetails is null)
        {
            throw new ArgumentException("Transfer details are required for transfer transactions.",
                paramName: nameof(transferDetails));
        }

        if (categorization.Type != TransactionType.Transfer && transferDetails is not null)
        {
            throw new ArgumentException("Transfer details are allowed only for transfer transactions.",
                paramName: nameof(transferDetails));
        }

        if (transferDetails?.ToWalletId == walletId)
        {
            throw new ArgumentException("Source and target wallet cannot be the same.",
                paramName: nameof(transferDetails));
        }
    }
}