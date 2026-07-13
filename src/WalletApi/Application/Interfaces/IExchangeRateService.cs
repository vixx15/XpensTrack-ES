namespace WalletApi.Application.Interfaces;

public interface IExchangeRateService
{
    Task<decimal> GetCurrentRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken = default);
    Task<decimal> GetHistoricalRateAsync(string fromCurrency, string toCurrency, DateTimeOffset date, CancellationToken cancellationToken = default);
}
