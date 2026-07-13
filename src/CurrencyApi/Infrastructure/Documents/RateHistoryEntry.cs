namespace CurrencyApi.Infrastructure.Documents;

public record RateHistoryEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string CurrencyCode { get; init; } = string.Empty;
    public string BaseCurrencyCode { get; init; } = string.Empty;
    public decimal Rate { get; init; }
    public DateTimeOffset RecordedAt { get; init; }
}
