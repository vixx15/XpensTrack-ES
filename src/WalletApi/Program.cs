using System.Globalization;
using System.Text;
using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using Marten;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.IdentityModel.Tokens;
using Shared;
using WalletApi.Projections;

var builder = WebApplication.CreateBuilder(args: args);
var martenConnectionString = builder.Configuration.GetConnectionString("wallet_db");
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret is not configured. Set Jwt__Secret environment variable or user-secret.");
var secretKey = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
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
