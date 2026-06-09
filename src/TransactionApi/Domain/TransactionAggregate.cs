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
            TransactionCategory: categorization.CategoryId,
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
            new TransactionCategorization(type: created.TransactionType, categoryId: created.TransactionCategory);
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
            OldTransactionCategory: Categorization.CategoryId,
            OldDescription: Description,
            OldOccuredAt: OccuredAt,
            OldToWalletId: TransferDetails?.ToWalletId,
            NewWalletId: newWallet.WalletId,
            NewWalletExchangeRate: newWallet.DefaultCurrencyConversion.ExchangeRate,
            NewWalletCurrencyCode: newWallet.DefaultCurrencyConversion.FromCurrencyCode,
            NewAmount: newAmount,
            NewDefaultCurrencyAmount: newDefaultCurrencyAmount,
            NewTransactionType: newCategorization.Type,
            NewTransactionCategory: newCategorization.CategoryId,
            NewDescription: newDescription,
            NewOccurredAt: newOccurredAt,
            NewToWalletId: newTransferDetails?.ToWalletId,
            NewToWalletAmount: toWalletMoney,
            NewToWalletCurrencyExchangeRate: newTransferDetails?.ToWalletConversion.ExchangeRate,
            NewToWalletCurrencyCode: newTransferDetails?.ToWalletConversion.ToCurrencyCode
        );


        /*
        if (newWallet != Wallet)
        {
            yield return new TransactionWalletUpdated(TransactionId: transactionId, WalletId: newWallet.WalletId,
                WalletExchangeRate: newWallet.DefaultCurrencyConversion.ExchangeRate,
                WalletCurrencyId: newWallet.DefaultCurrencyConversion.FromCurrencyCode);
        }

        if (newAmount != Money)
        {
            yield return new TransactionAmountUpdated(
                TransactionId: transactionId,
                PreviousAmount: Money,
                PreviousDefaultCurrencyAmount: GetDefaultCurrencyMoney(),
                NewAmount: newAmount,
                NewDefaultCurrencyAmount: Wallet.DefaultCurrencyConversion.Convert(newAmount),
                TransactionType: Categorization.Type,
                TransactionSubCategory: Categorization.CategoryId,
                OccuredAt: OccuredAt);
        }

        if (newOccurredAt != OccuredAt)
        {
            yield return new TransactionOccuredAtUpdated(
                TransactionId: transactionId,
                TransactionType: Categorization.Type,
                TransactionCategory: Categorization.CategoryId,
                PreviousOccuredAt: OccuredAt,
                NewOccuredAt: newOccurredAt,
                Amount: Money,
                DefaultCurrencyAmount: GetDefaultCurrencyMoney());
        }

        if (newDescription != null && newDescription != Description)
        {
            yield return new TransactionDescriptionUpdated(TransactionId: transactionId,
                Description: newDescription ?? Description);
        }

        if (newCategorization != Categorization)
        {
            if (newCategorization.Type != Categorization.Type)
            {
                yield return new TransactionTypeUpdated(
                    TransactionId: transactionId,
                    Amount: Money,
                    DefaultCurrencyAmount: GetDefaultCurrencyMoney(),
                    PreviousTransactionType: Categorization.Type,
                    NewTransactionType: newCategorization.Type,
                    PreviousTransactionCategory: Categorization.CategoryId,
                    NewTransactionCategory: newCategorization.CategoryId,
                    OccuredAt: OccuredAt);
            }
            else
            {
                if (Categorization.CategoryId != null)
                {
                    yield return new TransactionCategoryUpdated(
                        TransactionId: transactionId,
                        TransactionType: Categorization.Type,
                        PreviousTransactionCategory: Categorization.CategoryId ?? 0,
                        NewTransactionCategory: newCategorization.CategoryId ?? 0,
                        Amount: Money,
                        DefaultCurrencyAmount: GetDefaultCurrencyMoney(),
                        OccuredAt: OccuredAt);
                }
            }
        }


        if (newTransferDetails != TransferDetails)
        {
            yield return new TransactionWalletToUpdated(TransactionId: transactionId,
                WalletToId: newTransferDetails?.ToWalletId,
                WalletToExchangeRate: newTransferDetails?.ToWalletConversion.ExchangeRate,
                WalletToCurrencyId: newTransferDetails?.ToWalletConversion.ToCurrencyCode);
        }*/
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
            TransactionCategory: Categorization.CategoryId,
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
            new TransactionCategorization(type: e.NewTransactionType, categoryId: e.NewTransactionCategory);

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

    /*public void Apply(IEvent<TransactionDescriptionUpdated> descriptionUpdated)
    {
        Description = descriptionUpdated.Data.Description;
    }

    public void Apply(IEvent<TransactionAmountUpdated> amountUpdated)
    {
        Money = amountUpdated.Data.NewAmount;
    }

    public void Apply(IEvent<TransactionTypeUpdated> typeUpdated)
    {
        Categorization = new TransactionCategorization(typeUpdated.Data.NewTransactionType,
            typeUpdated.Data.NewTransactionCategory);
    }

    public void Apply(IEvent<TransactionCategoryUpdated> subcategoryUpdated)
    {
        Categorization = new TransactionCategorization(
            Categorization.Type,
            categoryId: subcategoryUpdated.Data.NewTransactionCategory
        );
    }

    public void Apply(IEvent<TransactionOccuredAtUpdated> occuredAtUpdated)
    {
        OccuredAt = occuredAtUpdated.Data.NewOccuredAt;
    }

    public void Apply(IEvent<TransactionWalletUpdated> walletUpdated)
    {
        var e = walletUpdated.Data;

        Money = Money with {
            CurrencyCode = e.WalletCurrencyId
        };

        Wallet = new WalletDetails(
            WalletId: e.WalletId,
            DefaultCurrencyConversion: new CurrencyConversion(
                ExchangeRate: e.WalletExchangeRate,
                FromCurrencyCode: e.WalletCurrencyId,
                ToCurrencyCode: Wallet.DefaultCurrencyConversion.ToCurrencyCode));
    }

    public void Apply(IEvent<TransactionWalletToUpdated> walletToUpdated)
    {
        var e = walletToUpdated.Data;

        if (e.WalletToId is null)
        {
            TransferDetails = null;
            return;
        }

        if (e.WalletToExchangeRate is null)
        {
            throw new InvalidOperationException("Transfer target exchange rate is missing.");
        }

        if (string.IsNullOrWhiteSpace(e.WalletToCurrencyId))
        {
            throw new InvalidOperationException("Transfer target currency is missing.");
        }

        TransferDetails = new TransferDetails(
            ToWalletId: e.WalletToId.Value,
            ToWalletConversion: new CurrencyConversion(
                ExchangeRate: e.WalletToExchangeRate.Value,
                FromCurrencyCode: Money.CurrencyCode,
                ToCurrencyCode: e.WalletToCurrencyId));
    }*/

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