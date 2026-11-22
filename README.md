![Pacer logo](https://raw.githubusercontent.com/WeekTwenty/Pacer/main/docs/logo.png)

# Pacer

**Load testing for .NET, without the baggage.**

[![NuGet](https://img.shields.io/nuget/v/Pacer.svg?logo=nuget&label=NuGet)](https://www.nuget.org/packages/Pacer)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](#license)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](#)

An open-source performance and load testing framework for the .NET ecosystem, built **strictly on
first-party Microsoft packages** — no third-party runtime dependencies.

Pacer lets you describe a test as a **scenario**: a pipeline of measured **steps** that each virtual
user runs as a journey, repeated under a realistic **load profile**. It reports throughput, latency
percentiles and error rates to the console, CSV, and a self-contained HTML page, and exposes live
metrics through .NET's `System.Diagnostics.Metrics` so you can watch a run with `dotnet-counters`.

## Features

- **Fluent scenario API** with one-time setup, a data pipeline between steps, and per-virtual-user state.
- **Five built-in load profiles** (closed model — you control the number of virtual users), chosen in
  `appsettings.json` or on the command line rather than in code:
  - `Load` — constant users for a duration.
  - `Soak` — constant users held for a long duration (endurance).
  - `Spike` — a baseline interrupted by a sudden surge.
  - `Stress` — a staircase that steps users up to find the breaking point.
  - `Ramp` — a bell curve: ramp up, hold, ramp down.
  - …or `Custom` stages of your own.
- **First-party stack**: `Microsoft.Extensions.Hosting`, dependency injection, logging, and `System.CommandLine`.
- **Live metrics** via a `Meter("Pacer")` plus bounded-memory HDR-style latency percentiles for reports.
- **CSV, console and HTML reports** out of the box, plus pluggable custom report writers.
- **Microsecond precision** — latencies below 5 ms render in µs, and CSV emits whole microseconds.
- **Baselines & history** — save named baselines (e.g. per release), compare runs against them, and track performance over time.
- **Deterministic engine** built on `TimeProvider`, so it is fully unit-tested without real delays.

## Try it (sample)

The repo ships a runnable sample app — the fastest way to see Pacer work:

```bash
git clone https://github.com/WeekTwenty/Pacer.git
cd Pacer

dotnet run --project src/Pacer.Sample -- list                                  # what can I run?
dotnet run --project src/Pacer.Sample -- run --scenario search                 # a 30s load test
DOTNET_ENVIRONMENT=Smoke dotnet run --project src/Pacer.Sample -- run --all    # a 10s smoke run of everything
```

The `run` prints a latency/throughput table and writes CSV, HTML and JSON reports under `./reports/`.

Each sample scenario shows a different part of the API:

| Scenario | Shows |
| --- | --- |
| `checkout` | Typed one-time setup, warm-up, a step's payload read by the next step (`ctx.PreviousAs<T>()`), failures with a status. |
| `orders` | A dependency resolved from `ctx.Services` (a fake API client registered in DI), setup that seeds data, `VirtualUserId` / `InvocationNumber`, the `(ctx, cancellationToken)` step overload, and a teardown that reads setup's data to clean up. |
| `search` | Counting transfer with `ctx.AddBytesReceived` instead of on the result. |

The sample also registers a custom `JsonReportWriter`, and `appsettings.Smoke.json` replaces the
load with a `custom` staged profile whenever `DOTNET_ENVIRONMENT=Smoke`.

### Compare against a baseline

```bash
# 1. On release 1.0: save the run as the "v1.0.0" baseline.
dotnet run --project src/Pacer.Sample -- run --scenario search --save-baseline v1.0.0

# 2. On the next release: run once, compare against "v1.0.0", and save this one as "v1.1.0".
dotnet run --project src/Pacer.Sample -- run --scenario search --baseline v1.0.0 --save-baseline v1.1.0 --fail-on-regression
```

The second command runs the test **once**, saves it as `v1.1.0`, and prints a diff table against
`v1.0.0` (baseline → current → % change). With `--fail-on-regression` it exits non-zero when anything
regressed past the tolerance.

## Use it in your app

Create a console app, reference `Pacer`, and hand your arguments to `PacerApplication`. A scenario
describes *what* a user does; *how much* load to apply lives in configuration (next section):

```csharp
using Microsoft.Extensions.DependencyInjection;
using Pacer.Hosting;
using Pacer.Scenarios;
using Pacer.Steps;

return await PacerApplication.RunAsync(args, builder =>
{
    builder.Services.AddPacer()
        .AddScenario(Scenario.Create("checkout")
            .InGroup("storefront")
            .AddStep("login",    async ctx => StepResult.Ok(payload: "token"))
            .AddStep("purchase", async ctx => StepResult.Ok()));
});
```

Then run it:

```bash
dotnet run -- list
dotnet run -- run --scenario checkout --out ./artifacts
dotnet run -- run --group storefront --profile stress --users 200 --duration 5m
```

### Configuring the load

The load profile is read from the `Pacer:Load` configuration section, so the same scenarios can
run as a quick smoke test locally and a long soak in CI without a code change. Add an
`appsettings.json` next to your project and copy it to the output directory:

```xml
<ItemGroup>
  <None Update="appsettings*.json" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

```json
{
  "Pacer": {
    "Load": { "Profile": "ramp", "Users": 100, "RampUp": "30s", "Hold": "1m", "RampDown": "30s" }
  }
}
```

Every scenario in a run uses this load. Anything left unset falls back to Pacer's default (`load`,
10 users, 1 minute), and the `run` flags (`--profile`, `--users`, `--duration`, `--warmup`) override
the configured values for that run.

Because it's standard .NET configuration, `appsettings.{Environment}.json` and environment variables
(`Pacer__Load__Users=200`, `Pacer__Load__Duration=10m`) work too.

Durations accept `500ms`, `30s`, `5m`, `1h30m` or `00:05:00`; a bare number is minutes. Settings per profile:

| Profile | Settings (all optional except where noted) |
| --- | --- |
| `load`, `soak` | `Users`, `Duration` |
| `ramp` | `Users` (peak), `RampUp`, `Hold`, `RampDown` — phases default to thirds of `Duration` |
| `spike` | `Users` (spike), `BaselineUsers` (default ¼), `SpikeDuration` (default ⅕ of `Duration`), `Duration` |
| `stress` | `Users` (max), `StartUsers`, `StepUsers` (default ⅕ of `Users`), `StepDuration` (default ⅕ of `Duration`) |
| `custom` | `Stages` (required): `[{ "StartUsers": 0, "EndUsers": 50, "Duration": "10s" }, …]` |

`Warmup` can be set too, and overrides the scenario's `WithWarmup(...)`. Overriding `Users` or
`Duration` on the command line re-derives the shape details that depend on it — e.g.
`--duration 10m` on the ramp above recomputes its phases, while `--users 500` keeps them.
`dotnet run -- list` shows the profile each scenario resolves to.

### One-time setup

Setup is defined inline on the scenario, so each scenario is self-contained. `WithSetup` runs once
before the load phase and its return value is exposed to every step via `ctx.ScenarioData`
(read it with `ctx.ScenarioDataAs<T>()`). Resolve dependencies through `ctx.Services`:

```csharp
Scenario.Create("checkout")
    .WithSetup(async ctx =>
    {
        // resolve services via ctx.Services, prepare auth tokens, seed data, …
        return new CheckoutData(AuthToken: "token-abc", ProductIds: [1000, 1001]);
    })
    .AddStep("login", async ctx =>
    {
        var data = ctx.ScenarioDataAs<CheckoutData>()!;
        return StepResult.Ok(payload: data.AuthToken);
    })
    /* … */
    .WithTeardown(async (ctx, ct) =>
    {
        // Runs once after the load phase, even if it failed, and sees what setup returned.
        var data = ctx.ScenarioDataAs<CheckoutData>()!;
        // … delete the seeded data
    });
```

Journeys still running when a virtual user is stopped (at the end of the run, or during a ramp-down)
are left out of the results rather than counted as failures, so error rates reflect real failures.

## CLI options (`run`)

| Option | Alias | Description |
| --- | --- | --- |
| `--scenario` | `-s` | Run a single named scenario. |
| `--group` | `-g` | Run every scenario in a group. |
| `--all` | | Run every registered scenario. |
| `--users` | `-u` | Override the configured peak number of virtual users. |
| `--duration` | `-d` | Override the configured duration, e.g. `30s`, `5m` (a bare number is minutes). |
| `--profile` | `-p` | Override the configured shape: `load`, `soak`, `spike`, `stress`, `ramp`. |
| `--warmup` | `-w` | Override the warm-up duration, e.g. `10s` (a bare number is minutes). |
| `--out` | `-o` | Output directory for file reports. |
| `--save-baseline [name]` | | Save the run as a named baseline, e.g. `v1.4.0` (default name `default`). |
| `--baseline [name]` | | Compare the run against a named baseline, e.g. `v1.3.0` (default `default`). |
| `--fail-on-regression` | | Return a non-zero exit code when a baseline regression is detected. |
| `--baseline-tolerance` | | Percent change beyond which a comparison counts as a regression (default `20`). |
| `--data-dir` | | Directory for baseline and history data (default `.pacer`). |

## Baselines & history

Every run is appended to history automatically (under `.pacer/` by default), so performance is tracked
across runs. To catch a regression, save a named baseline — a release is a natural name — then compare
later runs against it, all in the **same** `run` command:

```bash
# 1. When v1.3.0 ships: save it as a baseline.
dotnet run -- run --scenario checkout --save-baseline v1.3.0

# 2. For the next release: run once, compare against v1.3.0, and save this run as v1.4.0.
dotnet run -- run --scenario checkout --baseline v1.3.0 --save-baseline v1.4.0 --fail-on-regression
```

The comparison prints a diff table (baseline → current → % change) and marks any regression with `✗`.
The same comparison is added to the HTML report (a "vs baseline" section, plus a pass/regressed badge
by the scenario name) and written as `<scenario>-<timestamp>-baseline.csv`, so it's in the reports you
archive from CI, not just the build log. Comparing and saving under the same name in one run
(`--baseline v1.4.0 --save-baseline v1.4.0`) compares against the old `v1.4.0`, then replaces it.
With `--fail-on-regression`, the command exits non-zero when a metric is worse than the baseline by
more than `--baseline-tolerance` (default 20%).

Baselines are stored locally as readable JSON, one file per scenario and name, at
`.pacer/baselines/<scenario>/<name>.json` (change the root with `--data-dir`). Commit `.pacer/` if you
want baselines shared with your team or CI, or keep it ignored to keep them per machine. A baseline
records the load it ran under, and Pacer warns when you compare against one recorded with a different
profile or user count, since that comparison isn't like-for-like.

You can keep as many baselines per scenario as you like (`v1.3.0`, `before-refactor`, …). Inspect them
with:

```bash
dotnet run -- baseline list                         # scenarios with baselines
dotnet run -- baseline list --scenario checkout     # that scenario's baselines
dotnet run -- baseline show --scenario checkout --baseline v1.3.0
dotnet run -- baseline delete --scenario checkout --baseline v1.3.0

dotnet run -- history list
dotnet run -- history show --scenario checkout --last 5
```

The JSON-file stores are the defaults; swap them for another backend (e.g. SQLite) via the
`UseHistoryStore<T>()` / `UseBaselineStore<T>()` builder methods.

## Custom reports

Pacer ships console, CSV, and HTML writers, but reporting is fully extensible. Implement
`IReportWriter` and register it with `AddReportWriter<T>()` — every registered writer runs after
**every** test run:

```csharp
public sealed class JsonReportWriter : IReportWriter
{
    public string Format => "json";

    public async Task WriteAsync(IReadOnlyList<RunReport> reports, string outputDirectory, CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDirectory);
        foreach (var report in reports)
        {
            var path = Path.Combine(outputDirectory, $"{report.ScenarioName}.json");
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(report), ct);
        }
    }
}

builder.Services.AddPacer()
    .AddScenario(MyScenarios.Checkout())
    .AddReportWriter<JsonReportWriter>();
```

A writer receives the immutable `RunReport`s (per-step `StepStats`, the journey aggregate, interval
snapshots, and environment info) and the output directory.

## Live metrics

While a test runs, watch the `Pacer` meter:

```bash
dotnet-counters monitor --name <your-app> --counters Pacer
```

## Building & testing

```bash
dotnet build
dotnet test
```

## License

Apache-2.0.
