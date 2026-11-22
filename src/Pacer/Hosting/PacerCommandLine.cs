using System.CommandLine;
using Pacer.Load;

namespace Pacer.Hosting;

/// <summary>
/// Builds the Pacer command-line surface (the <c>run</c>, <c>list</c>, <c>baseline</c> and
/// <c>history</c> commands) and binds parsed arguments into option records. Kept internal and
/// separate from execution so the parsing and binding can be unit-tested without running a load test.
/// </summary>
internal sealed class PacerCommandLine
{
    private readonly Option<string?> _scenario = new("--scenario", "-s") { Description = "Name of a single scenario to run." };
    private readonly Option<string?> _group = new("--group", "-g") { Description = "Run every scenario in this group." };
    private readonly Option<bool> _all = new("--all") { Description = "Run every registered scenario." };
    private readonly Option<int> _users = new("--users", "-u") { Description = "Override the configured peak number of virtual users." };
    private readonly Option<TimeSpan?> _duration = CreateDurationOption("--duration", "-d", "Override the configured test duration, e.g. 30s, 5m, 1h30m (a bare number is minutes).");
    private readonly Option<string?> _profile = new("--profile", "-p") { Description = "Override the configured load profile: load, soak, spike, stress, or ramp." };
    private readonly Option<TimeSpan?> _warmup = CreateDurationOption("--warmup", "-w", "Override the configured warm-up duration, e.g. 10s, 1m (a bare number is minutes).");
    private readonly Option<string?> _out = new("--out", "-o") { Description = "Output directory for file-based reports." };
    private readonly Option<string?> _saveBaseline = new("--save-baseline") { Description = "Save the run as a baseline (optionally named).", Arity = ArgumentArity.ZeroOrOne };
    private readonly Option<string?> _baseline = new("--baseline") { Description = "Compare the run against a saved baseline (optionally named).", Arity = ArgumentArity.ZeroOrOne };
    private readonly Option<bool> _failOnRegression = new("--fail-on-regression") { Description = "Return a non-zero exit code when a baseline regression is detected." };
    private readonly Option<double> _baselineTolerance = CreateBaselineToleranceOption();
    private readonly Option<string?> _dataDir = new("--data-dir") { Description = "Directory for Pacer's baseline and history data.", Recursive = true };

    private readonly Option<string?> _baselineScenario = new("--scenario", "-s") { Description = "Scenario name.", Recursive = true };
    private readonly Option<string?> _baselineName = new("--baseline", "-b") { Description = "Baseline name.", Recursive = true, Arity = ArgumentArity.ZeroOrOne };
    private readonly Option<string?> _historyScenario = new("--scenario", "-s") { Description = "Scenario name.", Recursive = true };
    private readonly Option<int> _last = CreateLastOption();

    /// <summary>The configured root command.</summary>
    public RootCommand Root { get; }

    public PacerCommandLine(
        Func<RunOptions, CancellationToken, Task<int>> runHandler,
        Func<CancellationToken, Task<int>> listHandler,
        Func<BaselineOptions, CancellationToken, Task<int>> baselineHandler,
        Func<HistoryOptions, CancellationToken, Task<int>> historyHandler)
    {
        ArgumentNullException.ThrowIfNull(runHandler);
        ArgumentNullException.ThrowIfNull(listHandler);
        ArgumentNullException.ThrowIfNull(baselineHandler);
        ArgumentNullException.ThrowIfNull(historyHandler);

        var run = new Command("run", "Run one or more load-test scenarios.")
        {
            _scenario, _group, _all, _users, _duration, _profile, _warmup, _out,
            _saveBaseline, _baseline, _failOnRegression, _baselineTolerance,
        };
        run.SetAction((parseResult, cancellationToken) => runHandler(BindRun(parseResult), cancellationToken));

        var list = new Command("list", "List the registered scenarios and groups.");
        list.SetAction((_, cancellationToken) => listHandler(cancellationToken));

        var baseline = new Command("baseline", "Manage saved baselines.") { _baselineScenario, _baselineName };
        var baselineList = new Command("list", "List scenarios with a saved baseline, or a scenario's baselines.");
        baselineList.SetAction((parseResult, cancellationToken) => baselineHandler(
            new BaselineOptions { Action = BaselineAction.List, Scenario = NullIfBlank(parseResult.GetValue(_baselineScenario)), DataDirectory = DataDir(parseResult) }, cancellationToken));

        var baselineShow = new Command("show", "Show a saved baseline for a scenario.");
        baselineShow.SetAction((parseResult, cancellationToken) => baselineHandler(
            new BaselineOptions
            {
                Action = BaselineAction.Show,
                Scenario = NullIfBlank(parseResult.GetValue(_baselineScenario)),
                BaselineName = BaselineName(parseResult),
                DataDirectory = DataDir(parseResult),
            }, cancellationToken));

        var baselineDelete = new Command("delete", "Delete a saved baseline for a scenario.");
        baselineDelete.SetAction((parseResult, cancellationToken) => baselineHandler(
            new BaselineOptions
            {
                Action = BaselineAction.Delete,
                Scenario = NullIfBlank(parseResult.GetValue(_baselineScenario)),
                BaselineName = BaselineName(parseResult),
                DataDirectory = DataDir(parseResult),
            }, cancellationToken));

        baseline.Add(baselineList);
        baseline.Add(baselineShow);
        baseline.Add(baselineDelete);

        var history = new Command("history", "Inspect performance across runs.") { _historyScenario, _last };
        var historyList = new Command("list", "List scenarios with recorded history.");
        historyList.SetAction((parseResult, cancellationToken) => historyHandler(
            new HistoryOptions { Action = HistoryAction.List, DataDirectory = DataDir(parseResult) }, cancellationToken));

        var historyShow = new Command("show", "Show recent runs for a scenario.");
        historyShow.SetAction((parseResult, cancellationToken) => historyHandler(
            new HistoryOptions
            {
                Action = HistoryAction.Show,
                Scenario = NullIfBlank(parseResult.GetValue(_historyScenario)),
                Last = parseResult.GetValue(_last),
                DataDirectory = DataDir(parseResult),
            }, cancellationToken));

        history.Add(historyList);
        history.Add(historyShow);

        Root = new RootCommand("Pacer — a .NET performance and load testing framework.")
        {
            run, list, baseline, history, _dataDir,
        };
    }

    /// <summary>Maps a parsed result for the <c>run</c> command into <see cref="RunOptions"/>.</summary>
    public RunOptions BindRun(ParseResult parseResult)
    {
        ArgumentNullException.ThrowIfNull(parseResult);

        var users = parseResult.GetValue(_users);
        return new RunOptions
        {
            Scenario = NullIfBlank(parseResult.GetValue(_scenario)),
            Group = NullIfBlank(parseResult.GetValue(_group)),
            All = parseResult.GetValue(_all),
            Users = users > 0 ? users : null,
            Duration = parseResult.GetValue(_duration),
            Profile = NullIfBlank(parseResult.GetValue(_profile)),
            Warmup = parseResult.GetValue(_warmup),
            OutputDirectory = NullIfBlank(parseResult.GetValue(_out)) ?? "reports",
            SaveBaseline = NamedOption(parseResult, _saveBaseline),
            CompareBaseline = NamedOption(parseResult, _baseline),
            FailOnRegression = parseResult.GetValue(_failOnRegression),
            BaselineTolerancePercent = parseResult.GetValue(_baselineTolerance),
            DataDirectory = DataDir(parseResult),
        };
    }

    private string DataDir(ParseResult parseResult) => NullIfBlank(parseResult.GetValue(_dataDir)) ?? ".pacer";

    private string BaselineName(ParseResult parseResult) => NamedOption(parseResult, _baselineName) ?? "default";

    /// <summary>
    /// Resolves an optional-value option: <see langword="null"/> when it was not supplied, the trimmed
    /// value when one was given, or "default" when the flag was supplied without a value.
    /// </summary>
    private static string? NamedOption(ParseResult parseResult, Option<string?> option)
        => parseResult.GetResult(option) is null
            ? null
            : NullIfBlank(parseResult.GetValue(option)) ?? "default";

    private static Option<double> CreateBaselineToleranceOption()
    {
        var option = new Option<double>("--baseline-tolerance") { Description = "Percent change beyond which a baseline comparison counts as a regression." };
        option.DefaultValueFactory = _ => 20.0;
        return option;
    }

    private static Option<int> CreateLastOption()
    {
        var option = new Option<int>("--last") { Description = "Number of recent runs to show." };
        option.DefaultValueFactory = _ => 5;
        return option;
    }

    private static Option<TimeSpan?> CreateDurationOption(string name, string alias, string description) => new(name, alias)
    {
        Description = description,
        CustomParser = result =>
        {
            var token = result.Tokens.Count > 0 ? result.Tokens[0].Value : null;
            if (Durations.TryParse(token, out var duration))
                return duration;

            result.AddError($"Invalid duration '{token}' for {name}. Use a value such as 30s, 5m, 1h30m or 00:05:00 (a bare number is minutes).");
            return null;
        },
    };

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
