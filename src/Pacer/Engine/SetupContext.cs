using Microsoft.Extensions.Logging;
using Pacer.Scenarios;

namespace Pacer.Engine;

/// <summary>
/// The implementation of <see cref="ISetupContext"/> and <see cref="ITeardownContext"/> passed to
/// scenario setup and teardown. <see cref="ScenarioData"/> is filled in once setup returns.
/// </summary>
internal sealed class SetupContext : ITeardownContext
{
    public SetupContext(IServiceProvider services, ILogger logger, string scenarioName)
    {
        Services = services;
        Logger = logger;
        ScenarioName = scenarioName;
    }

    public IServiceProvider Services { get; }

    public ILogger Logger { get; }

    public string ScenarioName { get; }

    public object? ScenarioData { get; set; }
}
