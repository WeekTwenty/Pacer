using Pacer.Metrics;

namespace Pacer.History;

/// <summary>
/// A compact, serializable snapshot of one scenario run, used for baselines and cross-run history.
/// Captured from a <see cref="RunReport"/> by <see cref="FromReport"/>.
/// </summary>
public sealed record RunRecord
{
    /// <summary>The scenario that was run.</summary>
    public required string ScenarioName { get; init; }

    /// <summary>The scenario's group, if any.</summary>
    public string? Group { get; init; }

    /// <summary>The kind of load profile used.</summary>
    public required string LoadProfileKind { get; init; }

    /// <summary>The peak target virtual-user count of the load profile.</summary>
    public int PeakUsers { get; init; }

    /// <summary>When measurement started (after warm-up).</summary>
    public required DateTimeOffset StartedAt { get; init; }

    /// <summary>The measured duration (excludes warm-up).</summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>The machine and runtime the test ran on.</summary>
    public required EnvironmentInfo Environment { get; init; }

    /// <summary>Per-step statistics in declaration order.</summary>
    public required IReadOnlyList<StepStats> Steps { get; init; }

    /// <summary>Aggregate statistics for the full journey.</summary>
    public required StepStats Journey { get; init; }

    /// <summary>Creates a history record from a finished run's report.</summary>
    public static RunRecord FromReport(RunReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return new RunRecord
        {
            ScenarioName = report.ScenarioName,
            Group = report.Group,
            LoadProfileKind = report.LoadProfileKind,
            PeakUsers = report.PeakUsers,
            StartedAt = report.StartedAt,
            Duration = report.Duration,
            Environment = report.Environment,
            Steps = report.Steps,
            Journey = report.Journey,
        };
    }

    /// <summary>Reconstructs a report (without per-interval data) for display.</summary>
    public RunReport ToReport() => new()
    {
        ScenarioName = ScenarioName,
        Group = Group,
        LoadProfileKind = LoadProfileKind,
        PeakUsers = PeakUsers,
        StartedAt = StartedAt,
        Duration = Duration,
        Steps = Steps,
        Journey = Journey,
        Environment = Environment,
    };
}
