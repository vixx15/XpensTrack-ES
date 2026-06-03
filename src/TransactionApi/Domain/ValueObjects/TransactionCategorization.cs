using Shared;

namespace TransactionApi.Domain.ValueObjects;

public record TransactionCategorization
{
    public TransactionType Type { get; }
    public int? CategoryId { get; }

    public TransactionCategorization(TransactionType type, int? categoryId)
    {
        if (type == TransactionType.Transfer && categoryId is not null)
        {
            throw new ArgumentException("Transfer transaction cannot have category.", paramName: nameof(categoryId));
        }

        if (type != TransactionType.Transfer && categoryId is null)
        {
            throw new ArgumentException("Income and expense transactions must have category.",
                paramName: nameof(categoryId));
        }

        Type = type;
        CategoryId = categoryId;
    }

    public static TransactionCategorization Expense(int categoryId)
    {
        return new TransactionCategorization(type: TransactionType.Expense, categoryId: categoryId);
    }

    public static TransactionCategorization Income(int categoryId)
    {
        return new TransactionCategorization(type: TransactionType.Income, categoryId: categoryId);
    }

    public static TransactionCategorization Transfer()
    {
        return new TransactionCategorization(type: TransactionType.Transfer, categoryId: null);
    }
}