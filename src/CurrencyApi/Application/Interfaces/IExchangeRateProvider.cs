namespace CurrencyApi.Application.Interfaces;

public interface IExchangeRateProvider
{
    Task<Dictionary<string, decimal>> GetExchangeRates(string forCurrency,
        CancellationToken cancellationToken = default);
}