namespace TransactionApi.Infrastructure.Documents;

public record WalletReference
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string WalletType { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string CurrencyCode { get; init; } = string.Empty;
}
