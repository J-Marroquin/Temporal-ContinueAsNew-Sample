using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Temporal.ContinueAsNew.Worker.Activities.CsvActivities;
using Temporal.ContinueAsNew.Worker.Activities.Inventory;
using Temporal.ContinueAsNew.Worker.Configuration;

namespace Temporal.ContinueAsNew.Worker.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Temporal activity implementations as singletons.
    /// <para>
    /// Activities are registered by their concrete types (not interfaces) because
    /// Temporal relies on reflection to discover methods annotated with [Activity].
    /// </para>
    /// <para>
    /// The worker needs actual instances of the implementation classes to bind and execute activities.
    /// </para>
    /// <para>
    /// Interfaces are still used at the workflow level for type-safety and contracts,
    /// but they are not used by the DI container for activity resolution.
    /// </para>
    /// <para>
    /// Since activities are stateless and do not depend on scoped services (e.g., DbContext),
    /// they are registered as singletons.
    /// </para>
    /// </summary>
    public static IServiceCollection AddActivities(this IServiceCollection services)
    {
        // Activities
        services.AddSingleton<CsvActivities>();
        services.AddHttpClient<InventoryActivities>(client =>
        {
            client.BaseAddress = new Uri("http://bogus.inventory");
        });
        
        return services;
    }

    public static IServiceCollection AddConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        
        services.AddOptions<TemporalOptions>()
            .Bind(configuration.GetSection("Temporal"))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Host), "Temporal Host is required")
            .Validate(o => !string.IsNullOrWhiteSpace(o.TaskQueue), "TaskQueue is required")
            .ValidateOnStart();
        
        return services;
    }
}