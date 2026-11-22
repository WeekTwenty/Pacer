using Pacer.Load;
using Pacer.Scenarios;

namespace Pacer.Hosting;

/// <summary>
/// Decides what load each scenario runs with. Scenarios don't declare a load profile in code; it is
/// resolved from layers, each overriding the one before:
/// <list type="number">
/// <item>Pacer's built-in default (10 constant users for one minute).</item>
/// <item>The <c>Pacer:Load</c> configuration section.</item>
/// <item>The <c>run</c> command's <c>--profile</c>, <c>--users</c>, <c>--duration</c> and <c>--warmup</c> flags.</item>
/// </list>
/// Warm-up falls back to the scenario's <see cref="Scenario.WithWarmup"/> value when no layer sets it.
/// </summary>
public static class RunPlanner
{
    /// <summary>Returns a copy of <paramref name="scenario"/> with its resolved load profile and warm-up.</summary>
    /// <exception cref="LoadConfigurationException">The resolved configuration is invalid.</exception>
    public static ScenarioDefinition Apply(ScenarioDefinition scenario, RunOptions options, PacerSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            var plan = Resolve(options, settings);
            var result = scenario.WithLoad(plan.Build());
            return plan.Warmup is { } warmup ? result.WithWarmup(warmup) : result;
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException)
        {
            throw new LoadConfigurationException(scenario.Name, ex);
        }
    }

    internal static LoadPlan Resolve(RunOptions options, PacerSettings? settings)
    {
        return LoadPlan.Defaults
            .Overlay(LoadPlan.From(settings?.Load))
            .Overlay(new LoadPlan
            {
                Profile = options.Profile,
                Users = options.Users,
                Duration = options.Duration,
                Warmup = options.Warmup,
            });
    }
}
