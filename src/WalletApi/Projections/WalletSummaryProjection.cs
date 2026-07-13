using Marten.Events.Aggregation;
using WalletApi.Domain;
using WalletApi.Domain.Events;
using static WalletApi.Domain.BalanceMath;

namespace WalletApi.Projections;

public class WalletSummaryProjection : SingleStreamProjection<WalletSummary, Guid>
{
    public WalletSummaryProjection()
    {
        CreateEvent<WalletCreated>(creator: e =>
            new WalletSummary(
                Id: e.WalletId,
                Name: e.Name,
                Amount: e.OpeningBalance.Value,
                CurrencyCode: e.OpeningBalance.CurrencyCode,
                WalletTypeId: (int)e.WalletType,
                "",
                DefaultCurrencyAmount: e.DefaultCurrencyOpeningBalance.Value,
                DefaultCurrencyCode: e.DefaultCurrencyOpeningBalance.CurrencyCode,
                e.UserId)
        );

        ProjectEvent<WalletNameChanged>((summary, e) =>
            summary with { Name = e.NewName });

        ProjectEvent<WalletTypeChanged>((summary, e) =>
            summary with { WalletTypeId = (int)e.NewType });

        ProjectEvent<FundsMoved>((summary, e) =>
            summary with {
                Amount = summary.Amount + SignedDelta(e.Amount.Value, e.Direction),
                DefaultCurrencyAmount = summary.DefaultCurrencyAmount + SignedDelta(e.DefaultCurrencyAmount.Value, e.Direction)
            });

        ProjectEvent<FundsMovementReverted>((summary, e) =>
            summary with {
                Amount = summary.Amount - SignedDelta(e.Amount.Value, e.OriginalDirection),
                DefaultCurrencyAmount = summary.DefaultCurrencyAmount - SignedDelta(e.DefaultCurrencyAmount.Value, e.OriginalDirection)
            });

        ProjectEvent<FundsMovementAdjusted>((summary, e) =>
            summary with {
                Amount = summary.Amount - SignedDelta(e.OldAmount.Value, e.OldDirection) + SignedDelta(e.NewAmount.Value, e.NewDirection),
                DefaultCurrencyAmount = summary.DefaultCurrencyAmount - SignedDelta(e.OldDefaultCurrencyAmount.Value, e.OldDirection) + SignedDelta(e.NewDefaultCurrencyAmount.Value, e.NewDirection)
            });
    }
}