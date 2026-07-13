using System.Globalization;
using System.Text;
using CurrencyApi.Api;
using CurrencyApi.Application.Interfaces;
using CurrencyApi.Infrastructure;
using JasperFx;
using Marten;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args: args);

builder.WebHost.ConfigureKestrel(o => o.ConfigureEndpointDefaults(o => o.Protocols = HttpProtocols.Http1AndHttp2));

var martenConnectionString = builder.Configuration.GetConnectionString("currency_db");
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

builder.Services
    .AddLocalization()
    .AddMarten(configure: options =>
    {
        options.Connection(connectionString: martenConnectionString!);
        options.AutoCreateSchemaObjects = AutoCreate.All;
        options.CreateDatabasesForTenants(configure: c =>
        {
            c.ForTenant()
                .CheckAgainstPgDatabase()
                .WithOwner("postgres");
        });
    })
    .UseLightweightSessions();

builder.Services.AddGrpc();

builder.Services.AddMediatR(configuration: cfg =>
{
    cfg.RegisterServicesFromAssembly(assembly: typeof(Program).Assembly);
});

builder.Services.AddHttpClient<IExchangeRateProvider, ExchangeRateApiProvider>();
builder.Services.AddHostedService<ExchangeRateFetchWorker>();

var app = builder.Build();

app.MapGrpcService<ExchangeRateGrpcService>();

app.UseRequestLocalization(options =>
{
    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = [CultureInfo.GetCultureInfo("en"), CultureInfo.GetCultureInfo("sr-Latn")];
    options.SupportedUICultures = [CultureInfo.GetCultureInfo("en"), CultureInfo.GetCultureInfo("sr-Latn")];
});

app.UseAuthentication();
app.UseAuthorization();

app.Run();
