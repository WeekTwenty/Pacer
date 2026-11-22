namespace Pacer.History;

/// <summary>
/// The result of comparing a run against a named baseline: every comparable metric (not just the
/// regressions) plus what the baseline was recorded under. Attached to
/// <see cref="Metrics.RunReport.Baseline"/> when the run is compared, so every report writer can show it.
/// </summary>
public sealed record BaselineComparison
{
    /// <summary>The baseline's name, e.g. <c>v1.3.0</c>.</summary>
    public required string BaselineName { get; init; }

    /// <summary>Percent change beyond which a metric counts as a regression.</summary>
    public required double TolerancePercent { get; init; }

    /// <summary>When the baseline run started measuring.</summary>
    public required DateTimeOffset BaselineStartedAt { get; init; }

    /// <summary>The load profile kind the baseline was recorded with.</summary>
    public required string BaselineLoadProfileKind { get; init; }

    /// <summary>The peak users the baseline was recorded with.</summary>
    public required int BaselinePeakUsers { get; init; }

    /// <summary>True when the baseline used the same profile kind and peak users as this run.</summary>
    public required bool IsLikeForLike { get; init; }

    /// <summary>Every comparable metric, per step and for the journey.</summary>
    public required IReadOnlyList<MetricComparison> Metrics { get; init; }

    /// <summary>How many metrics regressed past the tolerance.</summary>
    public int RegressionCount => Metrics.Count(m => m.Regressed);

    /// <summary>True when any metric regressed past the tolerance.</summary>
    public bool HasRegressions => Metrics.Any(m => m.Regressed);

    /// <summary>Compares <paramref name="current"/> with <paramref name="baseline"/>.</summary>
    public static BaselineComparison Create(RunRecord current, RunRecord baseline, string baselineName, double tolerancePercent)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentException.ThrowIfNullOrWhiteSpace(baselineName);

        return new BaselineComparison
        {
            BaselineName = baselineName,
            TolerancePercent = tolerancePercent,
            BaselineStartedAt = baseline.StartedAt,
            BaselineLoadProfileKind = baseline.LoadProfileKind,
            BaselinePeakUsers = baseline.PeakUsers,
            IsLikeForLike = string.Equals(baseline.LoadProfileKind, current.LoadProfileKind, StringComparison.OrdinalIgnoreCase)
                && baseline.PeakUsers == current.PeakUsers,
            Metrics = BaselineComparer.Compare(current, baseline, tolerancePercent),
        };
    }
}
