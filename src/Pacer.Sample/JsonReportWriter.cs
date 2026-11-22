using System.Text.Json;
using Pacer.Metrics;
using Pacer.Reporting;

namespace Pacer.Sample;

/// <summary>
/// A custom report format: writes each run as <c>&lt;scenario&gt;-&lt;timestamp&gt;.json</c> next to the
/// built-in CSV and HTML reports. Registered with <c>AddReportWriter&lt;JsonReportWriter&gt;()</c>, it
/// runs after every test run alongside the console, CSV and HTML writers.
/// </summary>
public sealed class JsonReportWriter : IReportWriter
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <inheritdoc />
    public string Format => "json";

    /// <inheritdoc />
    public async Task WriteAsync(IReadOnlyList<RunReport> reports, string outputDirectory, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(outputDirectory);

        foreach (var report in reports)
        {
            // A slim, stable shape rather than the whole report, so downstream tools don't break
            // when Pacer adds fields.
            var document = new
            {
                scenario = report.ScenarioName,
                group = report.Group,
                profile = report.LoadProfileKind,
                peakUsers = report.PeakUsers,
                startedAt = report.StartedAt,
                durationSeconds = report.Duration.TotalSeconds,
                journey = Summarise(report.Journey),
                steps = report.Steps.Select(Summarise),
                baseline = report.Baseline is { } comparison
                    ? new
                    {
                        name = comparison.BaselineName,
                        tolerancePercent = comparison.TolerancePercent,
                        likeForLike = comparison.IsLikeForLike,
                        regressions = comparison.RegressionCount,
                        metrics = comparison.Metrics.Select(m => new
                        {
                            step = m.StepName,
                            metric = m.Metric,
                            baseline = Math.Round(m.BaselineValue, 6),
                            current = Math.Round(m.CurrentValue, 6),
                            percentChange = Math.Round(m.PercentChange, 2),
                            regressed = m.Regressed,
                        }),
                    }
                    : null,
            };

            var path = Path.Combine(outputDirectory, $"{report.ScenarioName}-{report.StartedAt:yyyyMMdd-HHmmss}.json");
            await using var stream = File.Create(path);
            await JsonSerializer.SerializeAsync(stream, document, Options, cancellationToken);
        }
    }

    private static object Summarise(StepStats step) => new
    {
        name = step.Name,
        ok = step.Ok,
        fail = step.Fail,
        rps = Math.Round(step.RequestsPerSecond, 2),
        meanMs = Math.Round(step.MeanMs, 3),
        p95Ms = Math.Round(step.P95Ms, 3),
        p99Ms = Math.Round(step.P99Ms, 3),
        maxMs = Math.Round(step.MaxMs, 3),
    };
}
