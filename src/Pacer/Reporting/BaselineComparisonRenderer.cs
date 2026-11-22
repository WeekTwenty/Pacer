using System.Text;
using Pacer.History;

namespace Pacer.Reporting;

/// <summary>
/// Renders a baseline comparison as an aligned text table (one row per step metric) so a run's
/// comparison against a named baseline is always visible — not just when a regression occurs.
/// </summary>
public static class BaselineComparisonRenderer
{
    /// <summary>Renders a run's comparison against its baseline.</summary>
    public static string Render(BaselineComparison comparison)
    {
        ArgumentNullException.ThrowIfNull(comparison);
        return Render(comparison.BaselineName, comparison.Metrics, comparison.TolerancePercent);
    }

    /// <summary>Renders the comparison of a run against the named baseline.</summary>
    public static string Render(string baselineName, IReadOnlyList<MetricComparison> comparisons, double tolerancePercent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baselineName);
        ArgumentNullException.ThrowIfNull(comparisons);

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine($" Compared against baseline '{baselineName}' (tolerance {tolerancePercent:0.#}%):");

        if (comparisons.Count == 0)
        {
            sb.AppendLine("   (no comparable metrics)");
            return sb.ToString();
        }

        var headers = new[] { "step", "metric", "baseline", "current", "change" };
        var rows = comparisons.Select(c => new[]
        {
            c.StepName,
            Label(c.Metric),
            Value(c.Metric, c.BaselineValue),
            Value(c.Metric, c.CurrentValue),
            Change(c.PercentChange) + (c.Regressed ? "  ✗" : ""),
        }).ToArray();

        var widths = new int[headers.Length];
        for (var i = 0; i < headers.Length; i++)
            widths[i] = headers[i].Length;
        foreach (var row in rows)
        {
            for (var i = 0; i < headers.Length; i++)
                widths[i] = Math.Max(widths[i], row[i].Length);
        }

        var ruleWidth = 1 + widths.Sum() + 2 * (headers.Length - 1);
        var rule = new string('─', ruleWidth);

        sb.Append(' ');
        for (var i = 0; i < headers.Length; i++)
        {
            if (i > 0)
                sb.Append("  ");
            sb.Append(i <= 1 ? headers[i].PadRight(widths[i]) : headers[i].PadLeft(widths[i]));
        }

        sb.AppendLine();
        sb.AppendLine(rule);

        foreach (var row in rows)
        {
            sb.Append(' ');
            for (var i = 0; i < headers.Length; i++)
            {
                if (i > 0)
                    sb.Append("  ");
                sb.Append(i <= 1 ? row[i].PadRight(widths[i]) : row[i].PadLeft(widths[i]));
            }

            sb.AppendLine();
        }

        sb.AppendLine(rule);

        var regressions = comparisons.Count(c => c.Regressed);
        sb.Append(regressions == 0
            ? " No regressions detected."
            : $" {regressions} regression{(regressions == 1 ? "" : "s")} detected.");

        return sb.ToString();
    }

    /// <summary>The display label for a comparison metric, e.g. <c>p95_ms</c> → <c>p95</c>.</summary>
    internal static string Label(string metric) => metric switch
    {
        "mean_ms" => "mean",
        "p95_ms" => "p95",
        "p99_ms" => "p99",
        "max_ms" => "max",
        "rps" => "rps",
        "fail_rate" => "fail rate",
        _ => metric,
    };

    /// <summary>Formats a comparison metric's value with its unit.</summary>
    internal static string Value(string metric, double value) => metric switch
    {
        "rps" => $"{ReportFormatting.Rps(value)} rps",
        "fail_rate" => $"{value * 100d:0.00}%",
        _ => ReportFormatting.Latency(value),
    };

    /// <summary>Formats a percent change with an explicit sign, e.g. <c>+12.5%</c>.</summary>
    internal static string Change(double percent)
        => percent > 0 ? $"+{percent:0.0}%" : percent < 0 ? $"{percent:0.0}%" : "0.0%";
}
