using JasperFx;
using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using Marten;
using TransactionApi.Projections;


var builder = WebApplication.CreateBuilder(args: args);
var martenConnectionString = builder.Configuration.GetConnectionString("transactions_db");
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
