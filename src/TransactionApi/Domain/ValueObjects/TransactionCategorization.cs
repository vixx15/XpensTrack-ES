namespace TransactionApi.Domain.ValueObjects;

public record TransactionCategorization
{
    public TransactionType Type { get; }
    public TransactionCategory? Category { get; }

    public TransactionCategorization(TransactionType type, TransactionCategory? category)
    {
        if (type == TransactionType.Transfer && category is not null)
        {
            throw new ArgumentException("Transfer transaction cannot have category.", paramName: nameof(category));
        }

        if (type != TransactionType.Transfer && category is null)
        {
            throw new ArgumentException("Income and expense transactions must have category.",
                paramName: nameof(category));
        }

        if (category is not null && !Enum.IsDefined(typeof(TransactionCategory), category.Value))
        {
            throw new ArgumentException($"Invalid category value: {category.Value}.", paramName: nameof(category));
        }

        if (category is not null && TransactionCategoryMapping.GetCategoryType(category.Value) != type)
        {
            throw new ArgumentException(
                $"Category '{category}' cannot be used with transaction type '{type}'.",
                paramName: nameof(category));
        }

        Type = type;
        Category = category;
    }

    public static TransactionCategorization Expense(TransactionCategory category)
    {
        return new TransactionCategorization(type: TransactionType.Expense, category: category);
    }

    public static TransactionCategorization Income(TransactionCategory category)
    {
        return new TransactionCategorization(type: TransactionType.Income, category: category);
    }

    public static TransactionCategorization Transfer()
    {
        return new TransactionCategorization(type: TransactionType.Transfer, category: null);
    }
}