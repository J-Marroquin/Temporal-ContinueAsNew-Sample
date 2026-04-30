using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Temporal.ContinueAsNew.Worker.Configuration;
using Temporal.ContinueAsNew.Worker.DependencyInjection;
using Temporalio.Client;
using Temporalio.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddConfiguration(builder.Configuration);
builder.Services.AddActivities();
builder.Services.AddTokenProvider();

var host = builder.Build();

var temporalOptions = host.Services
    .GetRequiredService<IOptions<TemporalOptions>>()
    .Value;

var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();

var client = await TemporalClient.ConnectAsync(new()
{
    TargetHost = temporalOptions.Host,
    LoggerFactory = loggerFactory,
});

var workerOptions = new TemporalWorkerOptions(temporalOptions.TaskQueue)
    .AddWorkflows(host.Services);

var worker = new TemporalWorker(client, workerOptions);

var logger = host.Services
    .GetRequiredService<ILogger<Program>>();

logger.LogInformation($"Worker listening on task queue: {temporalOptions.TaskQueue}");
logger.LogInformation($"Connected to Temporal at: {temporalOptions.Host}");

await worker.ExecuteAsync(host.Services
    .GetRequiredService<IHostApplicationLifetime>()
    .ApplicationStopping);