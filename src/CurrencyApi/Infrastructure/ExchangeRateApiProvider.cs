using System.Text.Json.Serialization;
using CurrencyApi.Application.Interfaces;

namespace CurrencyApi.Infrastructure;

public class ExchangeRateApiProvider(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<ExchangeRateApiProvider> logger)
    : IExchangeRateProvider
{
    public async Task<Dictionary<string, decimal>> GetExchangeRates(string forCurrency,
        CancellationToken cancellationToken = default)
    {
        var config = configuration.GetSection("ExchangeRateApi");
        var key = config["ApiKey"] ?? throw new InvalidOperationException("ExchangeRateApi:ApiKey is not configured.");
        var baseUrl = config["BaseUrl"] ?? throw new InvalidOperationException("ExchangeRateApi:BaseUrl is not configured.");

        logger.LogInformation("Fetching exchange rates for base currency {BaseCurrency}", forCurrency);

        try
        {
            var response = await httpClient
                .GetFromJsonAsync<ExchangeRateApiResponse>(
                    $"{baseUrl}{key}/latest/{forCurrency}", cancellationToken);

            if (response?.ConversionRates == null || response.ConversionRates.Count == 0)
                throw new InvalidOperationException("Exchange rate API returned an empty response.");

            if (response.Result == "error")
                throw new InvalidOperationException("Exchange rate API returned an error result.");

            return response.ConversionRates;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exchange rate API communication failed for {BaseCurrency}", forCurrency);
            throw new InvalidOperationException("Exchange Rate API communication failed.", ex);
        }
    }
}

public record ExchangeRateApiResponse(
    [property: JsonPropertyName("result")] string Result,
    [property: JsonPropertyName("documentation")] string Documentation,
    [property: JsonPropertyName("terms_of_use")] string TermsOfUse,
    [property: JsonPropertyName("time_last_update_unix")] long TimeLastUpdateUnix,
    [property: JsonPropertyName("time_last_update_utc")] string TimeLastUpdateUtc,
    [property: JsonPropertyName("time_next_update_unix")] long TimeNextUpdateUnix,
    [property: JsonPropertyName("time_next_update_utc")] string TimeNextUpdateUtc,
    [property: JsonPropertyName("base_code")] string BaseCode,
    [property: JsonPropertyName("conversion_rates")] Dictionary<string, decimal> ConversionRates
);
