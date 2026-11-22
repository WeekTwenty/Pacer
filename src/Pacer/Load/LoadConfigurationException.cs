namespace Pacer.Load;

/// <summary>Thrown when a scenario's configured load profile is invalid (unknown shape, bad duration, …).</summary>
public sealed class LoadConfigurationException : Exception
{
    /// <summary>Creates the exception for <paramref name="scenario"/>, wrapping the underlying cause.</summary>
    public LoadConfigurationException(string scenario, Exception innerException)
        : base($"Invalid load configuration for scenario '{scenario}': {innerException?.Message}", innerException)
    {
        Scenario = scenario;
    }

    /// <summary>The scenario whose load configuration is invalid.</summary>
    public string Scenario { get; }
}
