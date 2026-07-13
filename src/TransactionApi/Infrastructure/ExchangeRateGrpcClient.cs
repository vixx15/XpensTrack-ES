using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using TransactionApi.Application.Interfaces;
using XpensTrack.CurrencyApi.Api.Grpc;

namespace TransactionApi.Infrastructure;

public class ExchangeRateGrpcClient : IExchangeRateService
{
    private static readonly TimeSpan CallTimeout = TimeSpan.FromSeconds(5);

    private readonly ExchangeRateRpc.ExchangeRateRpcClient _client;

    public ExchangeRateGrpcClient(ExchangeRateRpc.ExchangeRateRpcClient client)
    {
        _client = client;
    }

    public async Task<decimal> GetCurrentRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken = default)
    {
        if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return 1m;
        }

        var request = new GetCurrentRateRequest
        {
            From = fromCurrency,
            To = toCurrency
        };

        var response = await _client.GetCurrentRateAsync(request,
            deadline: DateTime.UtcNow.Add(CallTimeout), cancellationToken: cancellationToken);
        return (decimal)response.Rate;
    }

    public async Task<decimal> GetHistoricalRateAsync(string fromCurrency, string toCurrency, DateTimeOffset date, CancellationToken cancellationToken = default)
    {
        if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return 1m;
        }

        var request = new GetHistoricalRateRequest
        {
            From = fromCurrency,
            To = toCurrency,
            Date = Timestamp.FromDateTime(date.UtcDateTime)
        };

        var response = await _client.GetHistoricalRateAsync(request,
            deadline: DateTime.UtcNow.Add(CallTimeout), cancellationToken: cancellationToken);
        return (decimal)response.Rate;
    }
}
