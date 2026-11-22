using System.Globalization;
using System.Text;
using Pacer.Metrics;

namespace Pacer.Reporting;

/// <summary>
/// Writes a machine-readable CSV of per-step statistics (one row per step plus a journey-total row
/// for each scenario) and, when available, a second CSV of per-interval throughput.
/// </summary>
public sealed class CsvReportWriter : IReportWriter
{
    /// <inheritdoc />
    public string Format => "csv";

    /// <inheritdoc />
    public async Task WriteAsync(IReadOnlyList<RunReport> reports, string outputDirectory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reports);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        Directory.CreateDirectory(outputDirectory);

        foreach (var report in reports)
        {
            var baseName = ReportFileName.ForScenario(report);
            await File.WriteAllTextAsync(Path.Combine(outputDirectory, $"{baseName}.csv"), RenderSummary([report]), cancellationToken).ConfigureAwait(false);

            if (report.Baseline is not null)
                await File.WriteAllTextAsync(Path.Combine(outputDirectory, $"{baseName}-baseline.csv"), RenderBaseline([report]), cancellationToken).ConfigureAwait(false);

            var intervals = RenderIntervals([report]);
            if (intervals is not null)
                await File.WriteAllTextAsync(Path.Combine(outputDirectory, $"{baseName}-intervals.csv"), intervals, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Renders the baseline comparison CSV for the reports that were compared: one row per step metric.
    /// Latency metrics (<c>*_ms</c>) are in milliseconds, <c>rps</c> in requests per second and
    /// <c>fail_rate</c> as a 0–1 fraction.
    /// </summary>
    public static string RenderBaseline(IReadOnlyList<RunReport> reports)
    {
        ArgumentNullException.ThrowIfNull(reports);

        var sb = new StringBuilder();
        sb.AppendLine("scenario,baseline,step,metric,baseline_value,current_value,percent_change,regressed");

        foreach (var report in reports)
        {
            if (report.Baseline is not { } comparison)
                continue;

            foreach (var metric in comparison.Metrics)
            {
                sb.Append(ReportFormatting.CsvEscape(report.ScenarioName)).Append(',')
                  .Append(ReportFormatting.CsvEscape(comparison.BaselineName)).Append(',')
                  .Append(ReportFormatting.CsvEscape(metric.StepName)).Append(',')
                  .Append(metric.Metric).Append(',')
                  .Append(metric.BaselineValue.ToString("0.######", CultureInfo.InvariantCulture)).Append(',')
                  .Append(metric.CurrentValue.ToString("0.######", CultureInfo.InvariantCulture)).Append(',')
                  .Append(metric.PercentChange.ToString("0.##", CultureInfo.InvariantCulture)).Append(',')
                  .AppendLine(metric.Regressed ? "true" : "false");
            }
        }

        return sb.ToString();
    }

    /// <summary>Renders the per-step summary CSV for all reports.</summary>
    public static string RenderSummary(IReadOnlyList<RunReport> reports)
    {
        ArgumentNullException.ThrowIfNull(reports);

        var sb = new StringBuilder();
        sb.AppendLine("scenario,row,ok,fail,total,rps,min_us,mean_us,p50_us,p75_us,p95_us,p99_us,p999_us,max_us,bytes_sent,bytes_received");

        foreach (var report in reports)
        {
            foreach (var step in report.Steps)
                AppendRow(sb, report.ScenarioName, step);
            AppendRow(sb, report.ScenarioName, report.Journey);
        }

        return sb.ToString();
    }

    /// <summary>Renders the per-interval throughput CSV, or <see langword="null"/> if no intervals were captured.</summary>
    public static string? RenderIntervals(IReadOnlyList<RunReport> reports)
    {
        ArgumentNullException.ThrowIfNull(reports);
        if (reports.All(r => r.Intervals.Count == 0))
            return null;

        var sb = new StringBuilder();
        sb.AppendLine("scenario,elapsed_s,active_users,ok,fail,rps");

        foreach (var report in reports)
        {
            foreach (var interval in report.Intervals)
            {
                sb.Append(ReportFormatting.CsvEscape(report.ScenarioName)).Append(',')
                  .Append(ReportFormatting.Seconds(interval.Elapsed)).Append(',')
                  .Append(ReportFormatting.Int(interval.ActiveUsers)).Append(',')
                  .Append(ReportFormatting.Int(interval.Ok)).Append(',')
                  .Append(ReportFormatting.Int(interval.Fail)).Append(',')
                  .Append(ReportFormatting.Rps(interval.RequestsPerSecond))
                  .AppendLine();
            }
        }

        return sb.ToString();
    }

    private static void AppendRow(StringBuilder sb, string scenario, StepStats step)
    {
        sb.Append(ReportFormatting.CsvEscape(scenario)).Append(',')
          .Append(ReportFormatting.CsvEscape(step.Name)).Append(',')
          .Append(ReportFormatting.Int(step.Ok)).Append(',')
          .Append(ReportFormatting.Int(step.Fail)).Append(',')
          .Append(ReportFormatting.Int(step.Total)).Append(',')
          .Append(ReportFormatting.Rps(step.RequestsPerSecond)).Append(',')
          .Append(ReportFormatting.Int(ReportFormatting.Microseconds(step.MinMs))).Append(',')
          .Append(ReportFormatting.Int(ReportFormatting.Microseconds(step.MeanMs))).Append(',')
          .Append(ReportFormatting.Int(ReportFormatting.Microseconds(step.P50Ms))).Append(',')
          .Append(ReportFormatting.Int(ReportFormatting.Microseconds(step.P75Ms))).Append(',')
          .Append(ReportFormatting.Int(ReportFormatting.Microseconds(step.P95Ms))).Append(',')
          .Append(ReportFormatting.Int(ReportFormatting.Microseconds(step.P99Ms))).Append(',')
          .Append(ReportFormatting.Int(ReportFormatting.Microseconds(step.P999Ms))).Append(',')
          .Append(ReportFormatting.Int(ReportFormatting.Microseconds(step.MaxMs))).Append(',')
          .Append(ReportFormatting.Int(step.BytesSent)).Append(',')
          .Append(ReportFormatting.Int(step.BytesReceived))
          .AppendLine();
    }
}
