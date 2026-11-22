namespace Pacer.Hosting;

/// <summary>The action requested of the <c>history</c> command.</summary>
public enum HistoryAction
{
    /// <summary>List scenarios with recorded history.</summary>
    List,

    /// <summary>Show recent runs for a scenario.</summary>
    Show,
}

/// <summary>The resolved options for a <c>history</c> command invocation.</summary>
public sealed record HistoryOptions
{
    /// <summary>The action to perform.</summary>
    public HistoryAction Action { get; init; }

    /// <summary>The scenario to show, if the action requires one.</summary>
    public string? Scenario { get; init; }

    /// <summary>Number of recent runs to show.</summary>
    public int Last { get; init; } = 5;

    /// <summary>Directory for Pacer's baseline and history data.</summary>
    public string DataDirectory { get; init; } = ".pacer";
}
