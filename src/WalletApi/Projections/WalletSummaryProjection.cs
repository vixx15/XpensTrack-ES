using Marten.Events.Aggregation;
using WalletApi.Domain.Events;

namespace WalletApi.Projections;

public class WalletSummaryProjection : SingleStreamProjection<WalletSummary, Guid>
{
    public WalletSummaryProjection()
    {
        CreateEvent<WalletCreated>(creator: e =>
            new WalletSummary(
                Id: e.WalletId,
                Name: e.Name,
                Amount: e.Amount.Value,
                CurrencyCode: e.Amount.CurrencyCode,
                WalletTypeId: e.WalletTypeId,
                "", //TODO
                DefaultCurrencyAmount: e.DefaultCurrencyAmount.Value,
                DefaultCurrencyCode: e.DefaultCurrencyAmount.CurrencyCode));

        ProjectEvent<WalletNameChanged>(handler: (summary, changed) =>
            summary with { Name = changed.NewName });

        ProjectEvent<WalletTypeChanged>(
            handler: (summary, changed) => summary with { WalletTypeId = changed.NewTypeId });
    }
}