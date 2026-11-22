using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pacer.Engine;
using Pacer.History;
using Pacer.Load;
using Pacer.Metrics;
using Pacer.Reporting;
using Pacer.Scenarios;

namespace Pacer.Hosting;

/// <summary>
/// The entry point for a Pacer console application. Builds a Generic Host (dependency injection,
/// configuration, console logging), wires the command-line surface, and dispatches the <c>run</c>,
/// <c>list</c>, <c>baseline</c> and <c>history</c> commands. Each scenario's load profile comes from the
/// <c>Pacer</c> configuration section (<c>appsettings.json</c> next to the executable, environment
/// variables, …) and the <c>run</c> flags — see <see cref="RunPlanner"/>. A consumer's <c>Program</c> typically
/// calls <see cref="RunAsync(string[], Action{HostApplicationBuilder}, CancellationToken)"/> and
/// registers scenarios via <see cref="PacerServiceCollectionExtensions.AddPacer"/>.
/// </summary>
public static class PacerApplication
{
    /// <summary>Builds the host, applies <paramref name="configure"/>, and runs the requested command.</summary>
    /// <example>
    /// A complete <c>Program.cs</c> — register scenarios, then dispatch the command-line arguments:
    /// <code><![CDATA[
    /// return await PacerApplication.RunAsync(args, builder =>
    /// {
    ///     builder.Services.AddPacer()
    ///         .AddScenario(SampleScenarios.Checkout());
    /// });
    ///
    /// // dotnet run -- list
    /// // dotnet run -- run --scenario checkout --out ./reports
    /// // dotnet run -- run --group storefront --profile stress --users 100 --duration 5m
    /// // dotnet run -- run --scenario checkout --save-baseline v1.4.0
    /// // dotnet run -- run --scenario checkout --baseline v1.4.0 --fail-on-regression
    /// // dotnet run -- baseline list
    /// // dotnet run -- history show --scenario checkout --last 5
    /// ]]></code>
    /// </example>
    public static async Task<int> RunAsync(string[] args, Action<HostApplicationBuilder> configure, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(configure);

        // Root content (appsettings*.json) at the executable so configuration is found however the app
        // is launched, e.g. `dotnet run --project …` from another directory or from a CI step.
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory,
        });
        builder.Logging.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
        });

        configure(builder);

        using var host = builder.Build();

        var cli = new PacerCommandLine(
            (options, ct) => RunCommandAsync(host.Services, options, ct),
            ct => ListCommandAsync(host.Services, ct),
            (options, ct) => BaselineCommandAsync(host.Services, options, ct),
            (options, ct) => HistoryCommandAsync(host.Services, options, ct));

        return await cli.Root.Parse(args).InvokeAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private static async Task<int> RunCommandAsync(IServiceProvider services, RunOptions options, CancellationToken cancellationToken)
    {
        var runner = services.GetRequiredService<TestRunner>();
        var history = services.GetRequiredService<IHistoryStore>();
        var baseline = services.GetRequiredService<IBaselineStore>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Pacer");
        if (!TryGetSettings(services, logger, out var settings))
            return 1;

        ScenarioDefinition Transform(ScenarioDefinition scenario) => RunPlanner.Apply(scenario, options, settings);

        IReadOnlyList<RunReport> reports;
        try
        {
            if (options.All)
                reports = await runner.RunAllAsync(Transform, cancellationToken).ConfigureAwait(false);
            else if (options.Group is not null)
                reports = await runner.RunGroupAsync(options.Group, Transform, cancellationToken).ConfigureAwait(false);
            else if (options.Scenario is not null)
                reports = await runner.RunScenarioAsync(options.Scenario, Transform, cancellationToken).ConfigureAwait(false);
            else
            {
                logger.LogError("Specify --scenario <name>, --group <name>, or --all.");
                return 1;
            }
        }
        catch (Exception ex) when (ex is KeyNotFoundException or LoadConfigurationException)
        {
            logger.LogError("{Message}", ex.Message);
            return 1;
        }

        // Compare against the named baseline first: before the report writers run, so every report can
        // show the comparison, and before anything is saved, so `--baseline X --save-baseline X`
        // compares against the old X rather than the run it's about to overwrite it with.
        if (options.CompareBaseline is not null)
        {
            var compared = new List<RunReport>(reports.Count);
            foreach (var report in reports)
            {
                var baselineRecord = await baseline.LoadAsync(options.DataDirectory, report.ScenarioName, options.CompareBaseline, cancellationToken).ConfigureAwait(false);
                if (baselineRecord is null)
                {
                    logger.LogWarning("No baseline '{Name}' saved for '{Scenario}'; skipping comparison.", options.CompareBaseline, report.ScenarioName);
                    compared.Add(report);
                    continue;
                }

                var comparison = BaselineComparison.Create(RunRecord.FromReport(report), baselineRecord, options.CompareBaseline, options.BaselineTolerancePercent);
                if (!comparison.IsLikeForLike)
                {
                    logger.LogWarning(
                        "Baseline '{Name}' for '{Scenario}' was recorded with a {BaselineProfile} profile at {BaselineUsers} users, but this run used {Profile} at {Users} users; the comparison is not like-for-like.",
                        options.CompareBaseline, report.ScenarioName, comparison.BaselineLoadProfileKind, comparison.BaselinePeakUsers, report.LoadProfileKind, report.PeakUsers);
                }

                compared.Add(report with { Baseline = comparison });
            }

            reports = compared;
        }

        // Every registered report writer (the built-in console/CSV/HTML plus any custom ones) runs.
        foreach (var writer in services.GetServices<IReportWriter>())
            await writer.WriteAsync(reports, options.OutputDirectory, cancellationToken).ConfigureAwait(false);

        var exitCode = 0;

        // Record every run in history and, when requested, promote it to a named baseline.
        foreach (var report in reports)
        {
            var record = RunRecord.FromReport(report);
            await history.AppendAsync(options.DataDirectory, record, cancellationToken).ConfigureAwait(false);

            if (options.SaveBaseline is not null)
            {
                await baseline.SaveAsync(options.DataDirectory, options.SaveBaseline, record, cancellationToken).ConfigureAwait(false);
                logger.LogInformation("Saved baseline '{Name}' for '{Scenario}'.", options.SaveBaseline, report.ScenarioName);
            }
        }

        // Always print a visible diff for each compared scenario, and flag regressions.
        foreach (var report in reports)
        {
            if (report.Baseline is not { } comparison)
                continue;

            Console.Out.WriteLine(BaselineComparisonRenderer.Render(comparison));

            foreach (var regression in comparison.Metrics.Where(c => c.Regressed))
            {
                logger.LogError("Regression for '{Scenario}' {Step} {Metric}: {Percent:0.0}% (baseline {Baseline:0.###}, current {Current:0.###}).",
                    report.ScenarioName, regression.StepName, regression.Metric,
                    regression.PercentChange, regression.BaselineValue, regression.CurrentValue);
                if (options.FailOnRegression)
                    exitCode = 1;
            }
        }

        return exitCode;
    }

    private static Task<int> ListCommandAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var registry = services.GetRequiredService<ScenarioRegistry>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Pacer");
        if (!TryGetSettings(services, logger, out var settings))
            return Task.FromResult(1);

        var scenarios = registry.All.OrderBy(s => s.Name).ToArray();
        if (scenarios.Length == 0)
        {
            logger.LogWarning("No scenarios are registered.");
            return Task.FromResult(0);
        }

        var exitCode = 0;
        foreach (var scenario in scenarios)
        {
            var group = scenario.Group is null ? "" : $" [group: {scenario.Group}]";
            try
            {
                var load = RunPlanner.Apply(scenario, new RunOptions(), settings).Load;
                logger.LogInformation("{Scenario}{Group} — {Steps} steps, {Profile} profile, {Users} users for {Duration}",
                    scenario.Name, group, scenario.Steps.Count, load.Kind, load.PeakUsers, Durations.Format(load.TotalDuration));
            }
            catch (LoadConfigurationException ex)
            {
                logger.LogError("{Scenario}{Group} — {Message}", scenario.Name, group, ex.Message);
                exitCode = 1;
            }
        }

        return Task.FromResult(exitCode);
    }

    private static bool TryGetSettings(IServiceProvider services, ILogger logger, out PacerSettings settings)
    {
        try
        {
            settings = services.GetRequiredService<IOptions<PacerSettings>>().Value;
            return true;
        }
        catch (InvalidOperationException ex)
        {
            // Raised by the configuration binder for values of the wrong type, e.g. "Users": "lots".
            logger.LogError("Invalid '{Section}' configuration: {Message}", PacerSettings.SectionName, ex.Message);
            settings = new PacerSettings();
            return false;
        }
    }

    private static async Task<int> BaselineCommandAsync(IServiceProvider services, BaselineOptions options, CancellationToken cancellationToken)
    {
        var store = services.GetRequiredService<IBaselineStore>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Pacer");

        switch (options.Action)
        {
            case BaselineAction.List:
                if (options.Scenario is null)
                {
                    foreach (var scenario in await store.ListScenariosAsync(options.DataDirectory, cancellationToken).ConfigureAwait(false))
                        logger.LogInformation("{Scenario}", scenario);
                }
                else
                {
                    foreach (var name in await store.ListBaselinesAsync(options.DataDirectory, options.Scenario, cancellationToken).ConfigureAwait(false))
                        logger.LogInformation("{Name}", name);
                }

                return 0;

            case BaselineAction.Show:
                if (options.Scenario is null)
                {
                    logger.LogError("Specify --scenario <name>.");
                    return 1;
                }

                var baseline = await store.LoadAsync(options.DataDirectory, options.Scenario, options.BaselineName, cancellationToken).ConfigureAwait(false);
                if (baseline is null)
                {
                    logger.LogWarning("No baseline '{Name}' saved for '{Scenario}'.", options.BaselineName, options.Scenario);
                    return 1;
                }

                Console.Out.WriteLine(ConsoleReportWriter.Render(baseline.ToReport()));
                return 0;

            case BaselineAction.Delete:
                if (options.Scenario is null)
                {
                    logger.LogError("Specify --scenario <name>.");
                    return 1;
                }

                await store.DeleteAsync(options.DataDirectory, options.Scenario, options.BaselineName, cancellationToken).ConfigureAwait(false);
                logger.LogInformation("Deleted baseline '{Name}' for '{Scenario}'.", options.BaselineName, options.Scenario);
                return 0;

            default:
                return 0;
        }
    }

    private static async Task<int> HistoryCommandAsync(IServiceProvider services, HistoryOptions options, CancellationToken cancellationToken)
    {
        var store = services.GetRequiredService<IHistoryStore>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Pacer");

        switch (options.Action)
        {
            case HistoryAction.List:
                foreach (var scenario in await store.ListScenariosAsync(options.DataDirectory, cancellationToken).ConfigureAwait(false))
                    logger.LogInformation("{Scenario}", scenario);
                return 0;

            case HistoryAction.Show:
                if (options.Scenario is null)
                {
                    logger.LogError("Specify --scenario <name>.");
                    return 1;
                }

                var records = await store.LoadAsync(options.DataDirectory, options.Scenario, cancellationToken).ConfigureAwait(false);
                if (records.Count == 0)
                {
                    logger.LogWarning("No history recorded for '{Scenario}'.", options.Scenario);
                    return 1;
                }

                foreach (var record in records.TakeLast(options.Last))
                    Console.Out.WriteLine(ConsoleReportWriter.Render(record.ToReport()));
                return 0;

            default:
                return 0;
        }
    }
}
