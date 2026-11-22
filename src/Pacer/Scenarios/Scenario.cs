using Pacer.Load;
using Pacer.Steps;

namespace Pacer.Scenarios;

/// <summary>
/// A fluent builder for a <see cref="ScenarioDefinition"/>. Start with <see cref="Create"/>, add
/// steps and configuration, then call <see cref="Build"/> (the engine and registry call it for you).
/// </summary>
/// <example>
/// A three-step checkout journey with inline one-time setup and a group. The setup runs once and its
/// result is read by every step via <see cref="Steps.StepContextExtensions.ScenarioDataAs{T}"/>.
/// The load profile is not part of the scenario: it comes from configuration (<c>Pacer:Load</c> in
/// <c>appsettings.json</c>) or the <c>run</c> command's flags —
/// see <see cref="Hosting.RunPlanner"/>.
/// <code><![CDATA[
/// var checkout = Scenario.Create("checkout")
///     .InGroup("storefront")
///     .WithSetup(async ctx => new CheckoutData(AuthToken: "token-abc", ProductIds: [1000, 1001, 1002]))
///     .WithWarmup(TimeSpan.FromSeconds(2))
///     .AddStep("login", async ctx =>
///     {
///         var data = ctx.ScenarioDataAs<CheckoutData>()!;
///         return StepResult.Ok(payload: data.AuthToken);
///     })
///     .AddStep("browse", async ctx => StepResult.Ok(bytesReceived: 4096))
///     .AddStep("purchase", async ctx => StepResult.Ok());
/// ]]></code>
/// </example>
public sealed class Scenario
{
    private readonly string _name;
    private readonly List<Step> _steps = [];
    private readonly HashSet<string> _stepNames = new(StringComparer.Ordinal);
    private string? _group;
    private ScenarioSetupDelegate? _setup;
    private ScenarioTeardownDelegate? _teardown;
    private TimeSpan _warmup = TimeSpan.Zero;

    private Scenario(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _name = name;
    }

    /// <summary>Begins defining a scenario with the given unique name.</summary>
    public static Scenario Create(string name) => new(name);

    /// <summary>Assigns the scenario to a group so it can be run via a group selector.</summary>
    public Scenario InGroup(string group)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(group);
        _group = group;
        return this;
    }

    /// <summary>
    /// Defines the one-time setup that runs before the load phase. Its return value is exposed to
    /// every step via <see cref="IStepContext.ScenarioData"/>. Resolve dependencies through
    /// <see cref="ISetupContext.Services"/>.
    /// </summary>
    public Scenario WithSetup(ScenarioSetupDelegate setup)
    {
        ArgumentNullException.ThrowIfNull(setup);
        _setup = setup;
        return this;
    }

    /// <summary>Defines the one-time setup that produces strongly-typed scenario data.</summary>
    public Scenario WithSetup<TData>(Func<ISetupContext, CancellationToken, ValueTask<TData>> setup)
    {
        ArgumentNullException.ThrowIfNull(setup);
        ScenarioSetupDelegate wrapped = async (context, cancellationToken) => (object?)await setup(context, cancellationToken).ConfigureAwait(false);
        return WithSetup(wrapped);
    }

    /// <summary>Defines the one-time setup that produces strongly-typed scenario data.</summary>
    public Scenario WithSetup<TData>(Func<ISetupContext, ValueTask<TData>> setup)
    {
        ArgumentNullException.ThrowIfNull(setup);
        return WithSetup<TData>((context, _) => setup(context));
    }

    /// <summary>Defines the one-time teardown that runs after the load phase completes.</summary>
    public Scenario WithTeardown(ScenarioTeardownDelegate teardown)
    {
        ArgumentNullException.ThrowIfNull(teardown);
        _teardown = teardown;
        return this;
    }

    /// <summary>Appends a step. Step names must be unique within the scenario.</summary>
    public Scenario AddStep(string name, StepDelegate execute)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(execute);
        if (!_stepNames.Add(name))
            throw new ArgumentException($"A step named '{name}' already exists in scenario '{_name}'.", nameof(name));

        _steps.Add(new Step(name, execute));
        return this;
    }

    /// <summary>Appends a step using a delegate that does not take the cancellation token directly.</summary>
    public Scenario AddStep(string name, Func<IStepContext, ValueTask<StepResult>> execute)
    {
        ArgumentNullException.ThrowIfNull(execute);
        return AddStep(name, (context, _) => execute(context));
    }

    /// <summary>Sets how long to run un-recorded warm-up journeys before measurement begins.</summary>
    public Scenario WithWarmup(TimeSpan warmup)
    {
        if (warmup < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(warmup), warmup, "Warm-up duration cannot be negative.");
        _warmup = warmup;
        return this;
    }

    /// <summary>
    /// Validates the configuration and produces an immutable <see cref="ScenarioDefinition"/>. The
    /// definition carries Pacer's default load profile until the host replaces it with the configured
    /// one (see <see cref="Hosting.RunPlanner"/>).
    /// </summary>
    public ScenarioDefinition Build()
    {
        if (_steps.Count == 0)
            throw new InvalidOperationException($"Scenario '{_name}' must define at least one step.");

        return new ScenarioDefinition(_name, _group, _steps.ToArray(), _setup, _teardown, _warmup, LoadPlan.Defaults.Build());
    }
}
