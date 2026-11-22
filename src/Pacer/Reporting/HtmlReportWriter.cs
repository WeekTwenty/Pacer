using System.Globalization;
using System.Net;
using System.Text;
using Pacer.History;
using Pacer.Metrics;

namespace Pacer.Reporting;

/// <summary>
/// Writes a single self-contained HTML report (inline CSS, inline SVG charts, no external scripts,
/// styles or fonts) summarising every scenario in the run.
/// </summary>
public sealed class HtmlReportWriter : IReportWriter
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    // Palette and type mirror the Pacer docs site (docs/index.html) for a consistent look.
    private const string Styles = """
        :root{--paper:#f4f2ec;--paper-2:#ebe8df;--ink:#17171a;--ink-2:#4a4a52;--ink-3:#85838a;--rule:#d6d2c7;--rule-strong:#17171a;--accent:#d9480f;--ok:#2b7a3d;--fail:#c92a2a;--users:#85838a;
          --sans:ui-sans-serif,system-ui,-apple-system,"Segoe UI",Roboto,sans-serif;--mono:ui-monospace,"SF Mono",Menlo,Consolas,"Liberation Mono",monospace}
        @media (prefers-color-scheme:dark){:root{--paper:#141416;--paper-2:#1f1f23;--ink:#ecebe6;--ink-2:#b4b2ad;--ink-3:#7a7880;--rule:#2c2c31;--rule-strong:#ecebe6;--accent:#ff6b2c;--ok:#5fbf75;--fail:#ff6b6b;--users:#7a7880}}
        *{box-sizing:border-box}
        body{margin:0;background:var(--paper);color:var(--ink);font-family:var(--sans);font-size:15px;line-height:1.55;-webkit-font-smoothing:antialiased}
        .wrap{max-width:1120px;margin:0 auto;padding:0 24px 64px}
        .top{display:flex;justify-content:space-between;align-items:baseline;gap:16px;flex-wrap:wrap;padding:20px 0;border-bottom:1px solid var(--rule);font-family:var(--mono);font-size:.82rem;color:var(--ink-3)}
        .mark{font-weight:600;font-size:.95rem;color:var(--ink)}
        .mark::before{content:"";display:inline-block;width:9px;height:9px;background:var(--accent);margin-right:9px}
        .scenario{padding:40px 0 8px}
        .scenario+.scenario{border-top:2px solid var(--rule-strong);margin-top:40px}
        .head{display:flex;justify-content:space-between;align-items:flex-end;gap:16px;flex-wrap:wrap;margin-bottom:20px}
        .eyebrow{font-family:var(--mono);font-size:.78rem;color:var(--ink-3);margin-bottom:4px}
        h1{font-size:clamp(1.8rem,4vw,2.5rem);letter-spacing:-.03em;line-height:1.1;margin:0;font-weight:700;overflow-wrap:anywhere}
        .meta{display:flex;flex-wrap:wrap;gap:6px 28px;margin:0 0 28px;font-size:.84rem}
        .meta div{display:flex;gap:8px}
        .meta dt{color:var(--ink-3)}.meta dd{margin:0}
        .kpis{display:grid;grid-template-columns:repeat(6,1fr);border-top:2px solid var(--rule-strong);border-bottom:1px solid var(--rule);margin-bottom:32px}
        .kpi{padding:14px 16px 16px 0}
        .kpi+.kpi{padding-left:16px;border-left:1px solid var(--rule)}
        .kpi .lbl{font-family:var(--mono);font-size:.72rem;text-transform:uppercase;letter-spacing:.06em;color:var(--ink-3)}
        .kpi .val{font-size:1.6rem;font-weight:600;letter-spacing:-.02em;font-variant-numeric:tabular-nums;white-space:nowrap}
        .kpi .val small{font-size:.8rem;font-weight:400;color:var(--ink-3);margin-left:3px;letter-spacing:0}
        .kpi.bad .val{color:var(--fail)}
        h2{font-size:1rem;font-weight:600;margin:0 0 10px;display:flex;justify-content:space-between;gap:12px;flex-wrap:wrap}
        h2 .note{font-family:var(--mono);font-size:.76rem;font-weight:400;color:var(--ink-3)}
        figure{margin:0 0 36px}
        .chart{display:block;width:100%;height:auto}
        .chart text{font-family:var(--mono);font-size:11px;fill:var(--ink-3)}
        .chart .grid{stroke:var(--rule);stroke-width:1}
        .chart .base{stroke:var(--ink-3);stroke-width:1}
        .chart .rps{fill:none;stroke:var(--accent);stroke-width:2;stroke-linejoin:round}
        .chart .area{fill:var(--accent);opacity:.08}
        .chart .users{fill:none;stroke:var(--users);stroke-width:1.5}
        .chart .users-area{fill:var(--users);opacity:.12}
        .legend{display:flex;gap:20px;font-family:var(--mono);font-size:.74rem;color:var(--ink-3);margin-top:6px}
        .legend i{display:inline-block;width:14px;height:2px;vertical-align:middle;margin-right:6px}
        .scroll{overflow-x:auto;margin-bottom:36px}
        table{border-collapse:collapse;width:100%;font-variant-numeric:tabular-nums;font-size:.88rem;border-top:2px solid var(--rule-strong)}
        th,td{padding:9px 10px;text-align:right;border-bottom:1px solid var(--rule);white-space:nowrap}
        th:first-child,td:first-child{text-align:left;padding-left:0}
        th:last-child,td:last-child{padding-right:0}
        thead th{font-family:var(--mono);font-weight:500;font-size:.72rem;text-transform:uppercase;letter-spacing:.06em;color:var(--ink-3)}
        th.grp{border-left:1px solid var(--rule)}td.grp{border-left:1px solid var(--rule)}
        tr.total td{font-weight:600;border-top:2px solid var(--rule-strong);border-bottom:2px solid var(--rule-strong)}
        td.fail{color:var(--fail)}td.zero{color:var(--ink-3)}
        .verdict{font-family:var(--mono);font-size:.8rem;padding:6px 10px;border:1px solid currentColor;white-space:nowrap}
        .verdict.ok{color:var(--ok)}.verdict.fail{color:var(--fail)}
        .warn{font-size:.86rem;color:var(--ink-2);border-left:3px solid var(--accent);padding:6px 12px;margin:0 0 14px;background:var(--paper-2)}
        table.cmp tr.step-start td{border-top:1px solid var(--rule-strong)}
        table.cmp tbody tr:first-child td{border-top:0}
        table.cmp tr.regressed td{color:var(--fail)}
        table.cmp th:nth-child(2),table.cmp td:nth-child(2){text-align:left}
        table.cmp td.flag{width:1.5em;padding-left:4px}
        footer{border-top:1px solid var(--rule);padding-top:16px;font-family:var(--mono);font-size:.76rem;color:var(--ink-3)}
        @media (max-width:820px){.kpis{grid-template-columns:repeat(3,1fr)}.kpi:nth-child(4){padding-left:0;border-left:0}.kpi:nth-child(-n+3){border-bottom:1px solid var(--rule)}}
        @media (max-width:520px){.wrap{padding:0 16px 48px}.kpis{grid-template-columns:repeat(2,1fr)}.kpi:nth-child(n){padding-left:16px;border-left:1px solid var(--rule);border-bottom:1px solid var(--rule)}.kpi:nth-child(odd){padding-left:0;border-left:0}}
        @media print{:root{--paper:#fff}.wrap{max-width:none}}
        """;

    /// <inheritdoc />
    public string Format => "html";

    /// <inheritdoc />
    public async Task WriteAsync(IReadOnlyList<RunReport> reports, string outputDirectory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reports);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        Directory.CreateDirectory(outputDirectory);

        foreach (var report in reports)
        {
            var fileName = $"{ReportFileName.ForScenario(report)}.html";
            await File.WriteAllTextAsync(Path.Combine(outputDirectory, fileName), Render([report]), cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Renders the complete HTML document for the given reports.</summary>
    public static string Render(IReadOnlyList<RunReport> reports)
    {
        ArgumentNullException.ThrowIfNull(reports);

        var title = reports.Count == 1 ? $"{reports[0].ScenarioName} · Pacer report" : "Pacer report";

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.Append("<title>").Append(Encode(title)).AppendLine("</title>");
        sb.Append("<style>").Append(Styles).AppendLine("</style></head><body><div class=\"wrap\">");

        sb.Append("<header class=\"top\"><span class=\"mark\">pacer</span><span>run report");
        if (reports.Count > 0)
            sb.Append(" · ").Append(Encode(reports[0].StartedAt.ToString("u", Invariant)));
        sb.AppendLine("</span></header>");

        foreach (var report in reports)
            AppendScenario(sb, report);

        sb.AppendLine("<footer>Generated by Pacer</footer>");
        sb.AppendLine("</div></body></html>");
        return sb.ToString();
    }

    private static void AppendScenario(StringBuilder sb, RunReport report)
    {
        sb.AppendLine("<section class=\"scenario\">");

        sb.Append("<div class=\"head\"><div><div class=\"eyebrow\">scenario");
        if (report.Group is not null)
            sb.Append(" · group: ").Append(Encode(report.Group));
        sb.Append("</div><h1>").Append(Encode(report.ScenarioName)).Append("</h1></div>");
        AppendVerdict(sb, report.Baseline);
        sb.AppendLine("</div>");

        sb.Append("<dl class=\"meta\">");
        AppendMeta(sb, "profile", report.LoadProfileKind);
        AppendMeta(sb, "peak users", ReportFormatting.Int(report.PeakUsers));
        AppendMeta(sb, "duration", ReportFormatting.HumanDuration(report.Duration));
        AppendMeta(sb, "started", report.StartedAt.ToString("u", Invariant));
        AppendMeta(sb, "machine", report.Environment.MachineName);
        AppendMeta(sb, "os", report.Environment.OperatingSystem);
        AppendMeta(sb, "runtime", report.Environment.RuntimeVersion);
        AppendMeta(sb, "cpus", ReportFormatting.Int(report.Environment.ProcessorCount));
        sb.AppendLine("</dl>");

        AppendSummary(sb, report.Journey);
        AppendChart(sb, report);
        AppendTable(sb, report);
        AppendBaseline(sb, report);

        sb.AppendLine("</section>");
    }

    private static void AppendVerdict(StringBuilder sb, BaselineComparison? comparison)
    {
        if (comparison is null)
            return;

        var regressions = comparison.RegressionCount;
        sb.Append(regressions == 0 ? "<div class=\"verdict ok\">no regressions vs " : "<div class=\"verdict fail\">")
          .Append(regressions == 0 ? "" : $"{regressions} regression{(regressions == 1 ? "" : "s")} vs ")
          .Append(Encode(comparison.BaselineName))
          .Append("</div>");
    }

    private static void AppendBaseline(StringBuilder sb, RunReport report)
    {
        if (report.Baseline is not { } comparison)
            return;

        sb.Append("<h2>vs baseline ").Append(Encode(comparison.BaselineName))
          .Append("<span class=\"note\">recorded ").Append(Encode(comparison.BaselineStartedAt.ToString("u", Invariant)))
          .Append(" · ").Append(Encode(comparison.BaselineLoadProfileKind))
          .Append(" · ").Append(ReportFormatting.Int(comparison.BaselinePeakUsers)).Append(" users")
          .Append(" · tolerance ").Append(comparison.TolerancePercent.ToString("0.#", Invariant)).Append('%')
          .AppendLine("</span></h2>");

        if (!comparison.IsLikeForLike)
        {
            sb.Append("<p class=\"warn\">The baseline ran a ").Append(Encode(comparison.BaselineLoadProfileKind))
              .Append(" profile at ").Append(ReportFormatting.Int(comparison.BaselinePeakUsers))
              .Append(" users; this run used ").Append(Encode(report.LoadProfileKind))
              .Append(" at ").Append(ReportFormatting.Int(report.PeakUsers))
              .AppendLine(" users, so the comparison isn't like-for-like.</p>");
        }

        if (comparison.Metrics.Count == 0)
        {
            sb.AppendLine("<p class=\"warn\">No comparable metrics.</p>");
            return;
        }

        sb.AppendLine("<div class=\"scroll\"><table class=\"cmp\"><thead><tr>");
        sb.Append("<th>step</th><th>metric</th><th>baseline</th><th>current</th><th>change</th><th></th>");
        sb.AppendLine("</tr></thead><tbody>");

        string? previousStep = null;
        foreach (var metric in comparison.Metrics)
        {
            var firstOfStep = !string.Equals(metric.StepName, previousStep, StringComparison.Ordinal);
            previousStep = metric.StepName;

            var classes = string.Join(' ', new[] { firstOfStep ? "step-start" : null, metric.Regressed ? "regressed" : null }.Where(c => c is not null));
            sb.Append(classes.Length == 0 ? "<tr>" : $"<tr class=\"{classes}\">");
            sb.Append("<td>").Append(firstOfStep ? Encode(metric.StepName) : "").Append("</td>");
            sb.Append("<td>").Append(Encode(BaselineComparisonRenderer.Label(metric.Metric))).Append("</td>");
            sb.Append("<td>").Append(Encode(BaselineComparisonRenderer.Value(metric.Metric, metric.BaselineValue))).Append("</td>");
            sb.Append("<td>").Append(Encode(BaselineComparisonRenderer.Value(metric.Metric, metric.CurrentValue))).Append("</td>");
            sb.Append("<td>").Append(Encode(BaselineComparisonRenderer.Change(metric.PercentChange))).Append("</td>");
            sb.Append("<td class=\"flag\">").Append(metric.Regressed ? "✗" : "").AppendLine("</td></tr>");
        }

        sb.AppendLine("</tbody></table></div>");
    }

    private static void AppendMeta(StringBuilder sb, string label, string value)
        => sb.Append("<div><dt>").Append(label).Append("</dt><dd>").Append(Encode(value)).Append("</dd></div>");

    private static void AppendSummary(StringBuilder sb, StepStats journey)
    {
        var total = journey.Ok + journey.Fail;
        var errorRate = total == 0 ? 0 : journey.Fail * 100d / total;

        sb.Append("<div class=\"kpis\">");
        AppendKpi(sb, "requests", ReportFormatting.IntGrouped(total), null, bad: false);
        AppendKpi(sb, "errors", errorRate.ToString(errorRate is > 0 and < 0.1 ? "0.00" : "0.0", Invariant), "%", bad: journey.Fail > 0);
        AppendKpi(sb, "throughput", ReportFormatting.Rps(journey.RequestsPerSecond), "rps", bad: false);
        AppendLatencyKpi(sb, "p50", journey.P50Ms);
        AppendLatencyKpi(sb, "p95", journey.P95Ms);
        AppendLatencyKpi(sb, "p99", journey.P99Ms);
        sb.AppendLine("</div>");
    }

    private static void AppendLatencyKpi(StringBuilder sb, string label, double ms)
    {
        // Latency() yields "<value> <unit>"; split it so the unit can be set smaller than the number.
        var formatted = ReportFormatting.Latency(ms);
        var space = formatted.LastIndexOf(' ');
        AppendKpi(sb, label, formatted[..space], formatted[(space + 1)..], bad: false);
    }

    private static void AppendKpi(StringBuilder sb, string label, string value, string? unit, bool bad)
    {
        sb.Append(bad ? "<div class=\"kpi bad\">" : "<div class=\"kpi\">")
          .Append("<div class=\"lbl\">").Append(label).Append("</div>")
          .Append("<div class=\"val\">").Append(Encode(value));
        if (unit is not null)
            sb.Append("<small>").Append(Encode(unit)).Append("</small>");
        sb.Append("</div></div>");
    }

    private static void AppendTable(StringBuilder sb, RunReport report)
    {
        sb.AppendLine("<h2>Steps</h2>");
        sb.AppendLine("<div class=\"scroll\"><table><thead><tr>");
        sb.Append("<th>step</th><th>ok</th><th>fail</th><th>rps</th>");
        sb.Append("<th class=\"grp\">in</th><th>out</th>");
        sb.Append("<th class=\"grp\">min</th><th>mean</th><th>p50</th><th>p95</th><th>p99</th><th>p99.9</th><th>max</th>");
        sb.AppendLine("</tr></thead><tbody>");

        foreach (var step in report.Steps)
            AppendRow(sb, step, isTotal: false);
        AppendRow(sb, report.Journey, isTotal: true);

        sb.AppendLine("</tbody></table></div>");
    }

    private static void AppendRow(StringBuilder sb, StepStats step, bool isTotal)
    {
        sb.Append(isTotal ? "<tr class=\"total\">" : "<tr>");
        sb.Append("<td>").Append(Encode(step.Name)).Append("</td>");
        // A zero count is dimmed; failures are the only thing coloured, so they stand out.
        sb.Append(step.Ok > 0 ? "<td>" : "<td class=\"zero\">").Append(ReportFormatting.IntGrouped(step.Ok)).Append("</td>");
        sb.Append(step.Fail > 0 ? "<td class=\"fail\">" : "<td class=\"zero\">").Append(ReportFormatting.IntGrouped(step.Fail)).Append("</td>");
        sb.Append("<td>").Append(ReportFormatting.Rps(step.RequestsPerSecond)).Append("</td>");
        sb.Append("<td class=\"grp\">").Append(ReportFormatting.Bytes(step.BytesReceived)).Append("</td>");
        sb.Append("<td>").Append(ReportFormatting.Bytes(step.BytesSent)).Append("</td>");
        sb.Append("<td class=\"grp\">").Append(ReportFormatting.Latency(step.MinMs)).Append("</td>");
        sb.Append("<td>").Append(ReportFormatting.Latency(step.MeanMs)).Append("</td>");
        sb.Append("<td>").Append(ReportFormatting.Latency(step.P50Ms)).Append("</td>");
        sb.Append("<td>").Append(ReportFormatting.Latency(step.P95Ms)).Append("</td>");
        sb.Append("<td>").Append(ReportFormatting.Latency(step.P99Ms)).Append("</td>");
        sb.Append("<td>").Append(ReportFormatting.Latency(step.P999Ms)).Append("</td>");
        sb.Append("<td>").Append(ReportFormatting.Latency(step.MaxMs)).Append("</td>");
        sb.AppendLine("</tr>");
    }

    private static void AppendChart(StringBuilder sb, RunReport report)
    {
        if (report.Intervals.Count < 2)
            return;

        var maxRps = report.Intervals.Max(i => i.RequestsPerSecond);
        if (maxRps <= 0)
            return;

        var lastElapsed = report.Intervals[^1].Elapsed.TotalSeconds;
        if (lastElapsed <= 0)
            return;

        // Geometry (viewBox units): a throughput panel on top and a slimmer active-users panel below,
        // sharing the time axis.
        const double width = 1000, left = 56, right = 8;
        const double rpsTop = 10, rpsHeight = 190;
        const double usersTop = rpsTop + rpsHeight + 22, usersHeight = 54;
        const double axisY = usersTop + usersHeight;
        const double height = axisY + 22;
        const double plotWidth = width - left - right;

        var rpsStep = NiceStep(maxRps / 5);
        var rpsTicks = (int)Math.Ceiling(maxRps / rpsStep);
        var rpsMax = rpsStep * rpsTicks;
        var usersMax = Math.Max(1, report.Intervals.Max(i => i.ActiveUsers));

        double X(double seconds) => left + seconds / lastElapsed * plotWidth;
        double RpsY(double rps) => rpsTop + rpsHeight - rps / rpsMax * rpsHeight;
        double UsersY(double users) => usersTop + usersHeight - users / usersMax * usersHeight;

        sb.Append("<h2>Throughput <span class=\"note\">requests per second over the run</span></h2>");
        sb.Append("<figure><svg class=\"chart\" viewBox=\"0 0 ").Append(N(width)).Append(' ').Append(N(height))
          .Append("\" role=\"img\" aria-label=\"throughput and active users over time\">");

        // Throughput gridlines and y labels.
        for (var tick = 0; tick <= rpsTicks; tick++)
        {
            var value = rpsStep * tick;
            var y = RpsY(value);
            sb.Append("<line class=\"").Append(tick == 0 ? "base" : "grid").Append("\" x1=\"").Append(N(left)).Append("\" x2=\"").Append(N(width - right))
              .Append("\" y1=\"").Append(N(y)).Append("\" y2=\"").Append(N(y)).Append("\"/>");
            sb.Append("<text x=\"").Append(N(left - 8)).Append("\" y=\"").Append(N(y + 4)).Append("\" text-anchor=\"end\">")
              .Append(AxisNumber(value)).Append("</text>");
        }

        // Throughput line and area.
        var rpsPoints = new StringBuilder();
        foreach (var interval in report.Intervals)
            rpsPoints.Append(N(X(interval.Elapsed.TotalSeconds))).Append(',').Append(N(RpsY(interval.RequestsPerSecond))).Append(' ');
        var firstX = N(X(report.Intervals[0].Elapsed.TotalSeconds));
        var baseY = N(RpsY(0));
        sb.Append("<path class=\"area\" d=\"M").Append(firstX).Append(',').Append(baseY).Append(" L")
          .Append(rpsPoints.ToString().TrimEnd().Replace(" ", " L", StringComparison.Ordinal))
          .Append(" L").Append(N(X(lastElapsed))).Append(',').Append(baseY).Append(" Z\"/>");
        sb.Append("<polyline class=\"rps\" points=\"").Append(rpsPoints.ToString().TrimEnd()).Append("\"/>");

        // Active users as a step line in its own panel.
        sb.Append("<line class=\"grid\" x1=\"").Append(N(left)).Append("\" x2=\"").Append(N(width - right))
          .Append("\" y1=\"").Append(N(usersTop)).Append("\" y2=\"").Append(N(usersTop)).Append("\"/>");
        sb.Append("<text x=\"").Append(N(left - 8)).Append("\" y=\"").Append(N(usersTop + 4)).Append("\" text-anchor=\"end\">")
          .Append(AxisNumber(usersMax)).Append("</text>");
        sb.Append("<text x=\"").Append(N(left - 8)).Append("\" y=\"").Append(N(axisY + 4)).Append("\" text-anchor=\"end\">0</text>");

        var usersPoints = new StringBuilder();
        var previousY = UsersY(0);
        usersPoints.Append(N(left)).Append(',').Append(N(previousY));
        foreach (var interval in report.Intervals)
        {
            var x = N(X(interval.Elapsed.TotalSeconds));
            var y = UsersY(interval.ActiveUsers);
            usersPoints.Append(' ').Append(x).Append(',').Append(N(previousY)).Append(' ').Append(x).Append(',').Append(N(y));
            previousY = y;
        }

        sb.Append("<path class=\"users-area\" d=\"M").Append(usersPoints.ToString().Replace(" ", " L", StringComparison.Ordinal))
          .Append(" L").Append(N(X(lastElapsed))).Append(',').Append(N(axisY)).Append(" Z\"/>");
        sb.Append("<polyline class=\"users\" points=\"").Append(usersPoints).Append("\"/>");

        // Shared time axis.
        sb.Append("<line class=\"base\" x1=\"").Append(N(left)).Append("\" x2=\"").Append(N(width - right))
          .Append("\" y1=\"").Append(N(axisY)).Append("\" y2=\"").Append(N(axisY)).Append("\"/>");
        for (var tick = 0; tick <= 4; tick++)
        {
            var seconds = lastElapsed * tick / 4;
            var anchor = tick == 0 ? "start" : tick == 4 ? "end" : "middle";
            sb.Append("<text x=\"").Append(N(X(seconds))).Append("\" y=\"").Append(N(axisY + 17)).Append("\" text-anchor=\"").Append(anchor).Append("\">")
              .Append(AxisTime(seconds)).Append("</text>");
        }

        sb.Append("</svg>");
        sb.Append("<div class=\"legend\"><span><i style=\"background:var(--accent)\"></i>throughput (rps)</span>")
          .Append("<span><i style=\"background:var(--users)\"></i>active users</span></div>");
        sb.AppendLine("</figure>");
    }

    /// <summary>Rounds a positive value up to a "nice" axis step (1, 2 or 5 × 10ⁿ).</summary>
    private static double NiceStep(double value)
    {
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(value)));
        foreach (var factor in new[] { 1d, 2d, 5d })
        {
            if (factor * magnitude >= value)
                return factor * magnitude;
        }

        return 10 * magnitude;
    }

    private static string AxisNumber(double value)
        => value >= 10_000 ? (value / 1000).ToString("0.#", Invariant) + "k" : value.ToString("0.#", Invariant);

    private static string AxisTime(double seconds)
    {
        var span = TimeSpan.FromSeconds(seconds);
        if (span.TotalSeconds < 60)
            return span.TotalSeconds.ToString("0", Invariant) + "s";
        if (span.TotalMinutes < 60)
            return span.Seconds == 0 ? $"{(int)span.TotalMinutes}m" : $"{(int)span.TotalMinutes}m{span.Seconds:00}s";
        return span.Minutes == 0 ? $"{(int)span.TotalHours}h" : $"{(int)span.TotalHours}h{span.Minutes:00}m";
    }

    private static string N(double value) => value.ToString("0.#", Invariant);

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
