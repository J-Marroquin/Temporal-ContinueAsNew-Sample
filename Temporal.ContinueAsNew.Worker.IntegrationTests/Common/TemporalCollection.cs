namespace Temporal.ContinueAsNew.Worker.IntegrationTests.Common;

[CollectionDefinition(nameof(TemporalCollection))]
public class TemporalCollection : ICollectionFixture<TemporalFixture> { }