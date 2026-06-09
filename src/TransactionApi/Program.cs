using System.Text;
using JasperFx;
using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using Marten;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TransactionApi.Projections;

var builder = WebApplication.CreateBuilder(args: args);
var martenConnectionString = builder.Configuration.GetConnectionString("transactions_db");
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

builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services.AddMarten(configure: options =>
    {
        options.Connection(connectionString: martenConnectionString!);
        options.AutoCreateSchemaObjects = AutoCreate.All;
        options.CreateDatabasesForTenants(configure: c =>
        {
            c.ForTenant()
                .CheckAgainstPgDatabase()
                .WithOwner("postgres");
        });
        options.Projections.Add<TransactionReadModelProjection>(lifecycle: ProjectionLifecycle.Inline);
        options.Projections.Add<MonthlyReportProjection>(lifecycle: ProjectionLifecycle.Async);
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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
