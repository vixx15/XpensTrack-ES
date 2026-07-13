using Grpc.Core;
using Marten;
using TransactionApi.Application.Interfaces;
using TransactionApi.Infrastructure.Cache;

namespace TransactionApi.Infrastructure;

public class ExchangeRateProvider(
    IExchangeRateService exchangeRateService,
    IDocumentStore documentStore,
    ILogger<ExchangeRateProvider> logger)
    : IExchangeRateProvider
{
    public async Task<decimal> GetRateAsync(
        string fromCurrency, string toCurrency,
        DateTimeOffset? date = null, CancellationToken cancellationToken = default)
    {
        if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase))
            return 1m;

        var effectiveDate = date ?? DateTimeOffset.UtcNow;
        var cacheId = $"{fromCurrency}_{toCurrency}_{effectiveDate:yyyyMMdd}";

        using var querySession = documentStore.QuerySession();
        var cached = await querySession.LoadAsync<CachedRateRecord>(cacheId, cancellationToken);

        if (cached != null)
        {
            logger.LogDebug("Cache hit for {CacheId}: rate {Rate}", cacheId, cached.Rate);
            return cached.Rate;
        }

        logger.LogInformation("Cache miss for {CacheId}, fetching from CurrencyApi", cacheId);

        decimal rate;
        try
        {
            rate = effectiveDate.Date == DateTimeOffset.UtcNow.Date
                ? await exchangeRateService.GetCurrentRateAsync(fromCurrency, toCurrency, cancellationToken)
                : await exchangeRateService.GetHistoricalRateAsync(fromCurrency, toCurrency, effectiveDate, cancellationToken);
        }
        catch (RpcException ex)
        {
            var stale = await querySession.Query<CachedRateRecord>()
                .Where(r => r.FromCurrency == fromCurrency && r.ToCurrency == toCurrency)
                .OrderByDescending(r => r.Date)
                .FirstOrDefaultAsync(cancellationToken);

            if (stale != null)
            {
                logger.LogWarning(ex, "CurrencyApi unreachable for {From}→{To}; using stale rate {Rate} from {Date}",
                    fromCurrency, toCurrency, stale.Rate, stale.Date);
                return stale.Rate;
            }

            logger.LogError(ex, "CurrencyApi unreachable for {From}→{To} and no stale rate available", fromCurrency, toCurrency);
            throw new InvalidOperationException($"Exchange rate for {fromCurrency}→{toCurrency} is unavailable.", ex);
        }

        using var session = documentStore.LightweightSession();
        session.Store(new CachedRateRecord
        {
            Id = cacheId,
            FromCurrency = fromCurrency,
            ToCurrency = toCurrency,
            Date = DateOnly.FromDateTime(effectiveDate.UtcDateTime),
            Rate = rate,
            CachedAt = DateTimeOffset.UtcNow
        });
        await session.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Cached rate {Rate} for {CacheId}", rate, cacheId);
        return rate;
    }
}
