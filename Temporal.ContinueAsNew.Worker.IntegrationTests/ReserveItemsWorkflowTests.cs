using FluentAssertions;
using Temporal.ContinueAsNew.Worker.IntegrationTests.Common;
using Temporal.ContinueAsNew.Worker.Models;
using Temporal.ContinueAsNew.Worker.Workflows;
using Xunit.Abstractions;
using Utils = Temporal.ContinueAsNew.Worker.IntegrationTests.Common.Utils;

namespace Temporal.ContinueAsNew.Worker.IntegrationTests;

[Collection(nameof(TemporalCollection))]
public class ReserveItemsWorkflowTests : TemporalIntegrationTestBase
{
    public ReserveItemsWorkflowTests(TemporalFixture fixture, ITestOutputHelper outputHelper)
        : base(fixture, outputHelper)
    {
        
    }
    
    [Fact(DisplayName = "Workflow should complete successfully")]
    public async Task Success()
    {
        // Arrange
        var csvPath = Utils.CreateTestCsv("test.csv", rows: 100);

        const int tenantId = 1;
        
        try
        {
            var importId = Utils.GetRandomHexHash();
            var input = new ReserveItemsInput(
                TenantId: tenantId,
                ImportId: importId,
                OrderId: "order-1",
                CsvPath: csvPath);

            // Act
            var handle = await Fixture.Client.StartWorkflowAsync(
                (ReserveItemsWorkflow wf) => wf.RunAsync(input),
                new()
                {
                    Id = $"wf-success-{Guid.NewGuid()}",
                    TaskQueue = Fixture.TaskQueue
                });

            var result = await handle.GetResultAsync();
            
            result.Should().NotBeNull();
            result.Code.Should().Be(0);
        }
        finally
        {
            // Cleanup
            if (File.Exists(csvPath))
            {
                File.Delete(csvPath);
            }
        }
    }
}