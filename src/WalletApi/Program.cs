using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using Marten;
using WalletApi.Projections;

var builder = WebApplication.CreateBuilder(args: args);
var martenConnectionString = builder.Configuration.GetConnectionString("wallet_db");

builder.Services
    .AddOpenApi()
    .AddControllers()
    .Services
    .AddMarten(configure: options =>
    {
        options.Connection(connectionString: martenConnectionString!);
        options.CreateDatabasesForTenants(configure: c =>
        {
            c.ForTenant()
                .CheckAgainstPgDatabase()
                .WithOwner("postgres");
        });
        options.Projections.Add<WalletSummaryProjection>(lifecycle: ProjectionLifecycle.Async);
        options.Projections.Add<AllWalletsOverviewProjection>(lifecycle: ProjectionLifecycle.Async);
    })    
    .AddAsyncDaemon(mode: DaemonMode.Solo)
    .UseLightweightSessions();


builder.Services.AddMediatR(configuration: cfg =>
{
    cfg.RegisterServicesFromAssembly(assembly: typeof(Program).Assembly);
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();