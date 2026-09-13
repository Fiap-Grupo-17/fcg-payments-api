using FCG.PaymentsAPI.Comum.Interfaces;
using FCG.PaymentsAPI.Consumers;
using FCG.PaymentsAPI.Mensageria;
using FCG.PaymentsAPI.Persistencia.Mongo;
using MassTransit;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// ── Idempotência de consumers (MongoDB) ─────────────────────────
var mongoOptions = new MongoOptions();
builder.Configuration.GetSection(MongoOptions.SectionName).Bind(mongoOptions);
builder.Services.AddSingleton(mongoOptions);
builder.Services.AddSingleton<MongoContext>();
builder.Services.AddSingleton<IProcessedEventStore, MongoProcessedEventStore>();
builder.Services.AddHostedService<MongoIndexInitializer>();

// ── MassTransit + RabbitMQ ──────────────────────────────────────
builder.Services.AddMassTransit(x =>
{
    // Prefixo de fila por serviço: evita colisão de nome de fila entre serviços,
    // garantindo fan-out (uma fila por serviço) em vez de competing consumers.
    x.SetEndpointNameFormatter(new DefaultEndpointNameFormatter("Payments", false));

    x.AddConsumer<OrderPlacedConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(
            builder.Configuration["RabbitMQ:Host"] ?? "localhost",
            builder.Configuration["RabbitMQ:VirtualHost"] ?? "/",
            h =>
            {
                h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
                h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
            });

        cfg.UseConsumeFilter(typeof(IdempotentConsumeFilter<>), ctx);

        cfg.ConfigureEndpoints(ctx);
    });
});

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "FCG.PaymentsAPI",
    timestamp = DateTime.UtcNow
}));

Log.Information("🚀 FCG.PaymentsAPI iniciando...");
app.Run();
