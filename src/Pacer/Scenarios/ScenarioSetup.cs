using Microsoft.Extensions.Logging;

namespace Pacer.Scenarios;

/// <summary>The context handed to a scenario's one-time setup.</summary>
public interface ISetupContext
{
    /// <summary>The service provider for resolving dependencies during setup.</summary>
    IServiceProvider Services { get; }

    /// <summary>A logger for the running scenario.</summary>
    ILogger Logger { get; }

    /// <summary>The name of the scenario being set up.</summary>
    string ScenarioName { get; }
}

/// <summary>
/// The context handed to a scenario's one-time teardown: everything setup had, plus the data setup
/// returned, so teardown can clean up exactly what was created.
/// </summary>
public interface ITeardownContext : ISetupContext
{
    /// <summary>The value returned by the scenario's setup, or <see langword="null"/> when there was none.</summary>
    object? ScenarioData { get; }
}

/// <summary>Convenience accessors for <see cref="ITeardownContext"/>.</summary>
public static class TeardownContextExtensions
{
    /// <summary>Returns the setup data cast to <typeparamref name="T"/>, or <see langword="default"/>.</summary>
    public static T? ScenarioDataAs<T>(this ITeardownContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.ScenarioData is T value ? value : default;
    }
}

/// <summary>
/// A scenario's one-time setup, defined inline on the scenario. It runs once before the load phase
/// and any value it returns is exposed to every step via
/// <see cref="Steps.IStepContext.ScenarioData"/> (e.g. an auth token or seeded test data).
/// </summary>
public delegate ValueTask<object?> ScenarioSetupDelegate(ISetupContext context, CancellationToken cancellationToken);

/// <summary>
/// A scenario's one-time teardown, run once after the load phase completes (even if it failed). Read
/// what setup created via <see cref="ITeardownContext.ScenarioData"/> or
/// <see cref="TeardownContextExtensions.ScenarioDataAs{T}"/>.
/// </summary>
public delegate ValueTask ScenarioTeardownDelegate(ITeardownContext context, CancellationToken cancellationToken);
