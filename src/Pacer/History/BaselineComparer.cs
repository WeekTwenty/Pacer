using Pacer.Metrics;

namespace Pacer.History;

/// <summary>A single metric compared against a baseline, with the change and whether it regressed.</summary>
public sealed record MetricComparison(
    string StepName,
    string Metric,
    double BaselineValue,
    double CurrentValue,
    double PercentChange,
    bool Regressed);

/// <summary>
/// Compares a current run against a stored baseline. Latency and error rate are "worse" when higher;
/// throughput is "worse" when lower. Every comparable metric is returned (not just regressions) so a
/// caller can render a full diff; filter on <see cref="MetricComparison.Regressed"/> for failures.
/// </summary>
public static class BaselineComparer
{
    private const double DefaultTolerancePercent = 20.0;

    /// <summary>Compares two runs, flagging metrics that changed beyond <paramref name="tolerancePercent"/>.</summary>
    public static IReadOnlyList<MetricComparison> Compare(
        RunRecord current, RunRecord baseline, double tolerancePercent = DefaultTolerancePercent)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentOutOfRangeException.ThrowIfNegative(tolerancePercent);

        var tolerance = tolerancePercent / 100d;
        var results = new List<MetricComparison>();

        var baselineSteps = baseline.Steps.ToDictionary(s => s.Name, StringComparer.Ordinal);
        foreach (var step in current.Steps)
        {
            if (baselineSteps.TryGetValue(step.Name, out var baselineStep))
                CompareStats(results, step, baselineStep, tolerance);
        }

        CompareStats(results, current.Journey, baseline.Journey, tolerance);
        return results;
    }

    private static void CompareStats(List<MetricComparison> results, StepStats current, StepStats baseline, double tolerance)
    {
        HigherIsWorse(results, current.Name, "mean_ms", current.MeanMs, baseline.MeanMs, tolerance);
        HigherIsWorse(results, current.Name, "p95_ms", current.P95Ms, baseline.P95Ms, tolerance);
        HigherIsWorse(results, current.Name, "p99_ms", current.P99Ms, baseline.P99Ms, tolerance);
        HigherIsWorse(results, current.Name, "max_ms", current.MaxMs, baseline.MaxMs, tolerance);

        LowerIsWorse(results, current.Name, "rps", current.RequestsPerSecond, baseline.RequestsPerSecond, tolerance);

        var currentFailRate = current.Total == 0 ? 0 : (double)current.Fail / current.Total;
        var baselineFailRate = baseline.Total == 0 ? 0 : (double)baseline.Fail / baseline.Total;
        HigherIsWorse(results, current.Name, "fail_rate", currentFailRate, baselineFailRate, tolerance);
    }

    private static void HigherIsWorse(
        List<MetricComparison> results, string step, string metric, double current, double baseline, double tolerance)
    {
        if (baseline <= 0)
            return;
        results.Add(new MetricComparison(step, metric, baseline, current, PercentChange(current, baseline), current > baseline * (1 + tolerance)));
    }

    private static void LowerIsWorse(
        List<MetricComparison> results, string step, string metric, double current, double baseline, double tolerance)
    {
        if (baseline <= 0)
            return;
        results.Add(new MetricComparison(step, metric, baseline, current, PercentChange(current, baseline), current < baseline * (1 - tolerance)));
    }

    private static double PercentChange(double current, double baseline)
        => baseline == 0 ? 0 : (current - baseline) / baseline * 100d;
}
