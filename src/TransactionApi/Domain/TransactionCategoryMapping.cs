namespace TransactionApi.Domain;

public static class TransactionCategoryMapping
{
    private static readonly HashSet<TransactionCategory> IncomeCategories =
    [
        TransactionCategory.Salary,
        TransactionCategory.Bonus,
        TransactionCategory.Gift,
        TransactionCategory.Freelance,
        TransactionCategory.Scholarship,
        TransactionCategory.Pension,
        TransactionCategory.Aid,
        TransactionCategory.Rent,
        TransactionCategory.Investment,
    ];

    private static readonly HashSet<TransactionCategory> ExpenseCategories =
    [
        TransactionCategory.House,
        TransactionCategory.Groceries,
        TransactionCategory.Transport,
        TransactionCategory.Utilities,
        TransactionCategory.Health,
        TransactionCategory.Hobby,
        TransactionCategory.Clothing,
        TransactionCategory.Restaurants,
        TransactionCategory.Electronics,
        TransactionCategory.Education,
        TransactionCategory.Children,
        TransactionCategory.Pets,
    ];

    public static TransactionType GetCategoryType(TransactionCategory category)
    {
        if (IncomeCategories.Contains(category))
            return TransactionType.Income;

        if (ExpenseCategories.Contains(category))
            return TransactionType.Expense;

        throw new ArgumentOutOfRangeException(nameof(category), category,
            $"Unknown category: {category}");
    }

    public static IReadOnlySet<TransactionCategory> GetCategories(TransactionType type) => type switch
    {
        TransactionType.Income => IncomeCategories,
        TransactionType.Expense => ExpenseCategories,
        TransactionType.Transfer => new HashSet<TransactionCategory>(),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, $"Unknown type: {type}")
    };
}
