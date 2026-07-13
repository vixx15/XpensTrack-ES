using System.Globalization;
using System.Text;
using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using Marten;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.IdentityModel.Tokens;
using WalletApi.Application.Interfaces;
using WalletApi.Infrastructure.Localization;
using WalletApi.Infrastructure;
using WalletApi.Infrastructure.Consumers;
using WalletApi.Infrastructure.ExceptionHandling;
using WalletApi.Infrastructure.Outbox;
using WalletApi.Projections;
using XpensTrack.CurrencyApi.Api.Grpc;

var builder = WebApplication.CreateBuilder(args: args);
var martenConnectionString = builder.Configuration.GetConnectionString("wallet_db");
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSettings["Secret"] ??
                throw new InvalidOperationException(
                    "JWT Secret is not configured. Set Jwt__Secret environment variable or user-secret.");
var secretKey = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(secretKey)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services
    .AddOpenApi()
    .AddControllers()
    .Services
    .AddLocalization()
    .AddSingleton<WalletTypeDisplayNames>()
    .AddMarten(configure: options =>
    {
        options.Connection(connectionString: martenConnectionString!);
        options.CreateDatabasesForTenants(configure: c =>
        {
            c.ForTenant()
                .CheckAgainstPgDatabase()
                .WithOwner("postgres");
        });

        options.Projections.Add<WalletSummaryProjection>(lifecycle: ProjectionLifecycle.Inline);
        options.Projections.Add<AllWalletsOverviewProjection>(lifecycle: ProjectionLifecycle.Async);
    })
    .AddAsyncDaemon(mode: DaemonMode.Solo)
    .UseLightweightSessions();

builder.Services.AddMediatR(configuration: cfg =>
{
    cfg.RegisterServicesFromAssembly(assembly: typeof(Program).Assembly);
});

builder.Services.AddHostedService<OutboxRelayService>();

builder.Services.AddGrpcClient<ExchangeRateRpc.ExchangeRateRpcClient>(o =>
{
    o.Address = new Uri(builder.Configuration["CurrencyApi:GrpcUrl"]!);
});
builder.Services.AddSingleton<IExchangeRateService, ExchangeRateGrpcClient>();
builder.Services.AddSingleton<IExchangeRateProvider, ExchangeRateProvider>();

var rabbitMqSettings = builder.Configuration.GetSection("RabbitMQ");
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<TransactionCreatedConsumer>();
    x.AddConsumer<TransactionUpdatedConsumer>();
    x.AddConsumer<TransactionDeletedConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitMqSettings["Host"], ushort.Parse(rabbitMqSettings["Port"]!), "/", h =>
        {
            h.Username(rabbitMqSettings["Username"]!);
            h.Password(rabbitMqSettings["Password"]!);
        });
        cfg.UseMessageRetry(r => r.Exponential(5,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(5)));
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseRequestLocalization(options =>
{
    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = [CultureInfo.GetCultureInfo("en"), CultureInfo.GetCultureInfo("sr-Latn")];
    options.SupportedUICultures = [CultureInfo.GetCultureInfo("en"), CultureInfo.GetCultureInfo("sr-Latn")];
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();