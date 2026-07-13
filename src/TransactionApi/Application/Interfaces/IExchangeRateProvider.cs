namespace TransactionApi.Application.Interfaces;

public interface IExchangeRateProvider
{
    Task<decimal> GetRateAsync(string fromCurrency, string toCurrency, DateTimeOffset? date = null, CancellationToken cancellationToken = default);
}
