using TransactionApi.Domain.Events;

namespace TransactionApi.Projections;

using Marten.Events.Aggregation;

public class TransactionReadModelProjection : SingleStreamProjection<TransactionReadModel, Guid>
{
    public TransactionReadModelProjection()
    {
        CreateEvent<TransactionCreated>(creator: e => new TransactionReadModel {
            Id = e.TransactionId,
            Amount = e.Amount.Value,
            Currency = e.Amount.CurrencyCode,
            TransactionType = e.TransactionType,
            CategoryId = (int?)e.TransactionCategory,
            Time = e.OccuredAt,
            UserId = e.UserId,
            Description = e.Description,
            WalletId = e.WalletId,

            DefaultCurrency = e.DefaultCurrencyAmount.CurrencyCode,
            DefaultCurrencyAmount = e.DefaultCurrencyAmount.Value,

            ToWalletId = e.ToWalletId,
            ToWalletCurrency = e.ToWalletCurrencyCode,
            ToWalletAmount = e.ToWalletAmount?.Value
        });

        DeleteEvent<TransactionDeleted>();
        
        ProjectEvent<TransactionUpdated>(handler: (item, e) =>
            item with {
                Amount = e.NewAmount.Value,
                Currency = e.NewWalletCurrencyCode,
                TransactionType = e.NewTransactionType,
                CategoryId = (int?)e.NewTransactionCategory,
                Time = e.NewOccurredAt,
                Description = e.NewDescription,
                WalletId = e.NewWalletId,

                DefaultCurrencyAmount = e.NewDefaultCurrencyAmount.Value,

                ToWalletId = e.NewToWalletId,
                ToWalletCurrency = e.NewToWalletCurrencyCode,
                ToWalletAmount = e.NewToWalletAmount?.Value
            });
    }
}