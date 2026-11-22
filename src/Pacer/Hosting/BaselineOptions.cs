namespace Pacer.Hosting;

/// <summary>The action requested of the <c>baseline</c> command.</summary>
public enum BaselineAction
{
    /// <summary>List scenarios with a saved baseline.</summary>
    List,

    /// <summary>Show the saved baseline for a scenario.</summary>
    Show,

    /// <summary>Delete the saved baseline for a scenario.</summary>
    Delete,
}

/// <summary>The resolved options for a <c>baseline</c> command invocation.</summary>
public sealed record BaselineOptions
{
    /// <summary>The action to perform.</summary>
    public BaselineAction Action { get; init; }

    /// <summary>The scenario to show or delete, if the action requires one.</summary>
    public string? Scenario { get; init; }

    /// <summary>The baseline name to show or delete.</summary>
    public string BaselineName { get; init; } = "default";

    /// <summary>Directory for Pacer's baseline and history data.</summary>
    public string DataDirectory { get; init; } = ".pacer";
}
