using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Temporal.ContinueAsNew.Worker.Activities.Inventory;
using Temporal.ContinueAsNew.Worker.Configuration;
using Temporal.ContinueAsNew.Worker.DependencyInjection;
using Temporalio.Client;
using Temporalio.Worker;
using Xunit.Abstractions;

namespace Temporal.ContinueAsNew.Worker.IntegrationTests.Common;

public class TemporalFixture : IAsyncLifetime
{
    private Task? _workerTask;
    private HostApplicationBuilder _builder = null!;
    private TemporalWorker _worker = null!;
    private CancellationTokenSource _cts = new();
    
    public string TaskQueue { get; private set; } = null!;
    public ITemporalClient Client { get; private set; } = null!;
    public IConfiguration Configuration { get; private set; } = null!;
    public IServiceProvider Services { get; private set; } = null!;
    
    public Task InitializeAsync()
    {
        _builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = "Development"
        });
        
        _builder.Logging.ClearProviders();
        _builder.Logging.AddConsole();
        
        _builder.Configuration
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables();


        Configuration = _builder.Configuration;
        
        _builder.Services.AddSingleton(Configuration);

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _cts.CancelAsync();

        if (_workerTask != null)
        {
            try
            {
                await _workerTask;
            }
            catch (OperationCanceledException)
            {
                // expected
            }
        }

        _worker.Dispose();
        _cts.Dispose();
    }
    
    public void BuildServices(ITestOutputHelper outputHelper, HttpMessageHandler? handler = null)
    {
        _builder.Logging.ClearProviders();
        _builder.Logging.AddConfiguration(Configuration.GetSection("Logging"));
        _builder.Logging.AddXUnit(outputHelper);
        _builder.Logging.AddConsole();

        _builder.Services.AddConfiguration(Configuration);
        _builder.Services.AddActivities();
        
        handler ??= new TestHttpMessageHandler();
        
        _builder.Services.AddHttpClient<InventoryActivities>()
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        
        var host = _builder.Build();
        Services = host.Services;
    }

    public async Task StartWorkerAsync(ITestOutputHelper outputHelper)
    {
        if (_workerTask != null)
            throw new InvalidOperationException("Worker already started");
        
        _cts = new CancellationTokenSource();
        
        var temporalOptions = Configuration
            .GetSection("Temporal")
            .Get<TemporalOptions>()!;
        
        var temporalHost = temporalOptions.Host;
        var taskQueue = temporalOptions.TaskQueue;

        if(string.IsNullOrWhiteSpace(temporalHost))
            throw new InvalidOperationException("Temporal host not configured");
        
        if(string.IsNullOrWhiteSpace(taskQueue))
            throw new InvalidOperationException("TaskQueue not configured");
        
        TaskQueue = taskQueue;
        
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddXUnit(outputHelper);
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddFilter("Temporalio.Activity", LogLevel.Warning);
        });
        
        Client = await TemporalClient.ConnectAsync(new()
        {
            TargetHost = temporalHost,
            LoggerFactory = loggerFactory
        });

        var workerOptions = new TemporalWorkerOptions(TaskQueue)
            .AddWorkflows(Services)
            .ConfigureOptions(temporalOptions);

        var worker = new TemporalWorker(Client, workerOptions);
        
        _worker = worker;
        _workerTask = Task.Run(() => _worker.ExecuteAsync(_cts.Token));
    }

}