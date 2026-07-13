using Marten.Schema;

namespace CurrencyApi.Infrastructure.Documents;

public record RateRecord
{
    [Identity] public string CurrencyCode { get; init; } = string.Empty;
    public string BaseCurrencyCode { get; init; } = string.Empty;
    public decimal Rate { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
