using TransactionApi.Domain;

namespace TransactionApi.Projections;

public record TransactionReadModel
{
    public Guid Id { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; }
    public TransactionType TransactionType { get; init; }

    public int? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public string? TypeName { get; init; }

    public DateTimeOffset Time { get; init; }
    public string UserId { get; init; }
    public string Description { get; init; }

    public Guid WalletId { get; init; }
    public string WalletName { get; init; }

    public decimal DefaultCurrencyAmount { get; init; }
    public string DefaultCurrency { get; init; }

    public Guid? ToWalletId { get; init; }
    public string? ToWalletName { get; init; }
    public string? ToWalletCurrency { get; init; }
    public decimal? ToWalletAmount { get; init; }
}