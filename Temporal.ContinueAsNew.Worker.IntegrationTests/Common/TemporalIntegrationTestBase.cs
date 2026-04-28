using Xunit.Abstractions;

namespace Temporal.ContinueAsNew.Worker.IntegrationTests.Common;

public class TemporalIntegrationTestBase : IAsyncLifetime
{
    protected readonly ITestOutputHelper OutputHelper;
    protected readonly TemporalFixture Fixture;
    protected HttpMessageHandler? Handler  { get; set; }
    
    public TemporalIntegrationTestBase(
        TemporalFixture fixture, 
        ITestOutputHelper outputHelper)
    {
        OutputHelper = outputHelper;
        Fixture = fixture;
    }
    
    public async Task InitializeAsync()
    {
        Fixture.BuildServices(OutputHelper, Handler);
        
        await Fixture.StartWorkerAsync(OutputHelper);
    }

    public async Task DisposeAsync()
    {
        await Task.CompletedTask;
    }
}