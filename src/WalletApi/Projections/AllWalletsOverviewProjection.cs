using Marten;
using Marten.Events.Projections;
using WalletApi.Domain;
using WalletApi.Domain.Events;
using static WalletApi.Domain.BalanceMath;

namespace WalletApi.Projections;

public class AllWalletsOverviewProjection : MultiStreamProjection<AllWalletsOverview, string>
{
    public AllWalletsOverviewProjection()
    {
        Identity<WalletCreated>(e => e.UserId);
        Identity<WalletNameChanged>(e => e.UserId);
        Identity<WalletTypeChanged>(e => e.UserId);
        Identity<FundsMoved>(e => e.UserId);
        Identity<FundsMovementReverted>(e => e.UserId);
        Identity<FundsMovementAdjusted>(e => e.UserId);
    }

    public AllWalletsOverview Create(WalletCreated @event)
    {
        return ApplyWalletCreated(
            new AllWalletsOverview(
                UserId: @event.UserId,
                WalletsByCategories: [],
                CurrencyCode: @event.DefaultCurrencyOpeningBalance.CurrencyCode),
            @event);
    }

    private static (WalletsByCategoryStats? Category, WalletSummary? Wallet) FindWalletInCurrentState(AllWalletsOverview current, Guid walletId)
    {
        foreach (var category in current.WalletsByCategories)
        {
            var wallet = category.Accounts.FirstOrDefault(w => w.Id == walletId);
            if (wallet != null) return (category, wallet);
        }
        return (null, null);
    }
    
    public AllWalletsOverview Apply(FundsMoved @event, AllWalletsOverview current)
    {
        var defaultDelta = SignedDelta(@event.DefaultCurrencyAmount.Value, @event.Direction);
        return ApplyBalanceDelta(current, @event.WalletId, SignedDelta(@event.Amount.Value, @event.Direction), defaultDelta);
    }

    public AllWalletsOverview Apply(FundsMovementReverted @event, AllWalletsOverview current)
    {
        var defaultDelta = -SignedDelta(@event.DefaultCurrencyAmount.Value, @event.OriginalDirection);
        return ApplyBalanceDelta(current, @event.WalletId, -SignedDelta(@event.Amount.Value, @event.OriginalDirection), defaultDelta);
    }

    public AllWalletsOverview Apply(FundsMovementAdjusted @event, AllWalletsOverview current)
    {
        var netDefaultDelta = SignedDelta(@event.NewDefaultCurrencyAmount.Value, @event.NewDirection)
                            - SignedDelta(@event.OldDefaultCurrencyAmount.Value, @event.OldDirection);
        var netAmountDelta = SignedDelta(@event.NewAmount.Value, @event.NewDirection)
                           - SignedDelta(@event.OldAmount.Value, @event.OldDirection);
        return ApplyBalanceDelta(current, @event.WalletId, netAmountDelta, netDefaultDelta);
    }

    private static AllWalletsOverview ApplyBalanceDelta(
        AllWalletsOverview current, Guid walletId, decimal amountDelta, decimal defaultCurrencyDelta)
    {
        var updatedCategories = current.WalletsByCategories.ToList();
        var (categoryStats, walletSummary) = FindWalletInCurrentState(current, walletId);
        if (categoryStats == null || walletSummary == null) return current;

        var originalSummary = walletSummary;
        walletSummary = walletSummary with {
            Amount = walletSummary.Amount + amountDelta,
            DefaultCurrencyAmount = walletSummary.DefaultCurrencyAmount + defaultCurrencyDelta
        };

        updatedCategories.Remove(categoryStats);
        categoryStats = RevertSummary(categoryStats, originalSummary);
        categoryStats = ApplySummary(categoryStats, walletSummary);
        updatedCategories.Add(categoryStats);

        return RecalculateGlobalTotals(current, updatedCategories);
    }

    public AllWalletsOverview Apply(WalletCreated @event, AllWalletsOverview current)
    {
        return ApplyWalletCreated(current, @event);
    }

    public AllWalletsOverview Apply(WalletNameChanged @event, AllWalletsOverview current)
    {
        var updatedCategories = current.WalletsByCategories.Select(category =>
        {
            var targetAccount = category.Accounts.FirstOrDefault(a => a.Id == @event.WalletId);
            if (targetAccount == null)
            {
                return category;
            }

            var updatedAccounts = category.Accounts.ToList();
            updatedAccounts.Remove(targetAccount);
            updatedAccounts.Add(targetAccount with { Name = @event.NewName });

            return category with { Accounts = updatedAccounts };
        }).ToList();

        return current with { WalletsByCategories = updatedCategories };
    }

    public AllWalletsOverview Apply(WalletTypeChanged @event, AllWalletsOverview current)
    {
        var targetAccount = current
            .WalletsByCategories
            .SelectMany(stats => stats.Accounts)
            .FirstOrDefault(a => a.Id == @event.WalletId);

        if (targetAccount == null)
        {
            return current;
        }

        var updatedCategories = current.WalletsByCategories.Select(category =>
        {
            if (category.CategoryId == (int)@event.OldType)
            {
                return RevertSummary(category, targetAccount);
            }

            if (category.CategoryId == (int)@event.NewType)
            {
                return ApplySummary(category, targetAccount with { WalletTypeId = (int)@event.NewType });
            }

            return category;
        }).Where(stats => !stats.Accounts.IsEmpty()).ToList();

        if (!updatedCategories.Exists(stats => stats.CategoryId == (int)@event.NewType))
        {
            updatedCategories.Add(ApplySummary(new WalletsByCategoryStats((int)@event.NewType, []),
                targetAccount with { WalletTypeId = (int)@event.NewType }));
        }

        return current with { WalletsByCategories = updatedCategories };
    }

    private AllWalletsOverview ApplyWalletCreated(
        AllWalletsOverview current, WalletCreated @event)
    {
        var updatedCategories = current.WalletsByCategories.ToList();
        
        var (existingCat, existingWallet) = FindWalletInCurrentState(current, @event.WalletId);
        if (existingCat != null && existingWallet != null)
        {
            updatedCategories.Remove(existingCat);
            var reverted = RevertSummary(existingCat, existingWallet);
            if (!reverted.Accounts.IsEmpty()) updatedCategories.Add(reverted);
        }

        var categoryStats = updatedCategories.FirstOrDefault(c => c.CategoryId == (int)@event.WalletType);
        var newWalletSummary = CreateNewWalletSummary(@event);

        if (categoryStats != null)
        {
            updatedCategories.Remove(categoryStats);
            updatedCategories.Add(ApplySummary(categoryStats, newWalletSummary));
        }
        else
        {
            updatedCategories.Add(ApplySummary(
                new WalletsByCategoryStats(CategoryId: (int)@event.WalletType, Accounts: []),
                newWalletSummary));
        }

        return RecalculateGlobalTotals(current, updatedCategories);
    }

    private static WalletsByCategoryStats ApplySummary(
        WalletsByCategoryStats category, WalletSummary summary)
    {
        var amount = summary.DefaultCurrencyAmount;
        var accounts = category.Accounts.ToList();

        var existingIndex = accounts.FindIndex(w => w.Id == summary.Id);
        if (existingIndex >= 0)
        {
            accounts[existingIndex] = summary;
            return category with { Accounts = accounts };
        }
        else
            accounts.Add(summary);

        return category with {
            Accounts = accounts,
            Total = category.Total + amount,
            Assets = amount > 0 ? category.Assets + amount : category.Assets,
            Liabilities = amount < 0 ? category.Liabilities + amount : category.Liabilities
        };
    }

    private static WalletsByCategoryStats RevertSummary(
        WalletsByCategoryStats category, WalletSummary summary)
    {
        var amount = summary.DefaultCurrencyAmount;
        var accounts = category.Accounts.ToList();
        var existing = accounts.FirstOrDefault(walletSummary => walletSummary.Id == summary.Id);
        if (existing == null) return category;
        accounts.Remove(existing);

        return category with {
            Accounts = accounts,
            Total = category.Total - amount,
            Assets = amount > 0 ? category.Assets - amount : category.Assets,
            Liabilities = amount < 0 ? category.Liabilities - amount : category.Liabilities
        };
    }

    private static WalletSummary CreateNewWalletSummary(
        WalletCreated @event
    )
    {
        return new WalletSummary(
            Id: @event.WalletId,
            Name: @event.Name,
            @event.OpeningBalance.Value,
            @event.OpeningBalance.CurrencyCode,
            (int)@event.WalletType,
            "",
            @event.DefaultCurrencyOpeningBalance.Value,
            @event.DefaultCurrencyOpeningBalance.CurrencyCode,
            @event.UserId);
    }
    
    private static AllWalletsOverview RecalculateGlobalTotals(AllWalletsOverview current, List<WalletsByCategoryStats> updatedCategories)
    {
        return current with {
            WalletsByCategories = updatedCategories,
            Total = updatedCategories.Sum(c => c.Total),
            Assets = updatedCategories.Sum(c => c.Assets),
            Liabilities = updatedCategories.Sum(c => c.Liabilities)
        };
    }
}