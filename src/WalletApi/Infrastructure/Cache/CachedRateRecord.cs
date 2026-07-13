namespace WalletApi.Infrastructure.Cache;

public record CachedRateRecord
{
    public string Id { get; init; } = string.Empty;  // "{from}_{to}_{yyyyMMdd}"
    public string FromCurrency { get; init; } = string.Empty;
    public string ToCurrency { get; init; } = string.Empty;
    public DateOnly Date { get; init; }
    public decimal Rate { get; init; }
    public DateTimeOffset CachedAt { get; init; }
}
