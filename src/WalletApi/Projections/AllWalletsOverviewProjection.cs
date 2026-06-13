using Marten;
using Marten.Events.Projections;
using WalletApi.Domain.Events;

namespace WalletApi.Projections;

public class AllWalletsOverviewProjection : MultiStreamProjection<AllWalletsOverview, string>
{
    public AllWalletsOverviewProjection()
    {
        Identity<WalletCreated>(e => e.UserId);
        Identity<WalletNameChanged>(e => e.UserId);
        Identity<WalletTypeChanged>(e => e.UserId);
    }

    public AllWalletsOverview Create(WalletCreated @event)
    {
        return ApplyWalletCreated(
            new AllWalletsOverview(
                UserId: @event.UserId,
                WalletsByCategories: [],
                CurrencyCode: @event.DefaultCurrencyAmount.CurrencyCode),
            @event);
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
                new WalletsByCategoryStats(
                    CategoryId: (int)@event.WalletType,
                    Accounts: []),
                newWalletSummary));
        }

        return current with {
            WalletsByCategories = updatedCategories,
            Total = updatedCategories.Sum(c => c.Total),
            Assets = updatedCategories.Sum(c => c.Assets),
            Liabilities = updatedCategories.Sum(c => c.Liabilities)
        };
    }

    private static WalletsByCategoryStats ApplySummary(
        WalletsByCategoryStats category, WalletSummary summary)
    {
        var amount = summary.DefaultCurrencyAmount;
        var accounts = category.Accounts.ToList();
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
        accounts.Remove(summary);

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
            @event.Amount.Value,
            @event.Amount.CurrencyCode,
            (int)@event.WalletType,
            "",
            @event.DefaultCurrencyAmount.Value,
            @event.DefaultCurrencyAmount.CurrencyCode);
    }
}
