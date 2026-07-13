using CurrencyApi.Application.Interfaces;
using CurrencyApi.Infrastructure.Documents;
using Marten;

namespace CurrencyApi.Infrastructure;

public class ExchangeRateFetchWorker(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<ExchangeRateFetchWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await FetchIfStale(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromDays(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await FetchAndStoreRates(stoppingToken);
        }
    }

    private async Task FetchIfStale(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IDocumentStore>();

        using var querySession = store.QuerySession();
        var mostRecent = await querySession.Query<RateRecord>()
            .OrderByDescending(r => r.UpdatedAt)
            .Select(r => r.UpdatedAt)
            .FirstOrDefaultAsync(stoppingToken);

        if (mostRecent != default && mostRecent > DateTimeOffset.UtcNow.AddDays(-1))
        {
            logger.LogInformation("Rates last updated at {Timestamp}, skipping startup fetch", mostRecent);
            return;
        }

        await FetchAndStoreRates(stoppingToken);
    }

    private async Task FetchAndStoreRates(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IExchangeRateProvider>();
        var store = scope.ServiceProvider.GetRequiredService<IDocumentStore>();

        try
        {
            var baseCurrency = configuration["ExchangeRateApi:BaseCurrency"]
                ?? throw new InvalidOperationException("ExchangeRateApi:BaseCurrency is not configured.");

            var rates = await provider.GetExchangeRates(baseCurrency, stoppingToken);
            var now = DateTimeOffset.UtcNow;

            using var session = store.LightweightSession();

            foreach (var (currencyCode, rate) in rates)
            {
                if (rate <= 0)
                {
                    logger.LogWarning("Skipping invalid rate for {Currency}: {Rate}", currencyCode, rate);
                    continue;
                }

                session.Store(new RateRecord
                {
                    CurrencyCode = currencyCode,
                    BaseCurrencyCode = baseCurrency,
                    Rate = rate,
                    UpdatedAt = now
                });

                session.Insert(new RateHistoryEntry
                {
                    CurrencyCode = currencyCode,
                    BaseCurrencyCode = baseCurrency,
                    Rate = rate,
                    RecordedAt = now
                });
            }

            await session.SaveChangesAsync(stoppingToken);
            logger.LogInformation("Exchange rates updated successfully at {Timestamp}", now);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching exchange rates");
        }
    }
}
