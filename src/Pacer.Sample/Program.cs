using Microsoft.Extensions.DependencyInjection;
using Pacer.Hosting;
using Pacer.Sample;

// A Pacer console app: register Pacer, its scenarios and their dependencies, then hand the
// command-line arguments to PacerApplication. The load profile comes from appsettings.json (plus
// appsettings.{Environment}.json), and the run flags override it. Try:
//   dotnet run -- list
//   dotnet run -- run --scenario checkout --out ./artifacts
//   dotnet run -- run --group storefront --profile stress --users 100 --duration 2m
//   dotnet run -- run --scenario search --save-baseline v1.0.0
//   dotnet run -- run --scenario search --baseline v1.0.0 --fail-on-regression
//   DOTNET_ENVIRONMENT=Smoke dotnet run -- run --all      (a 10s custom-shaped smoke run)
return await PacerApplication.RunAsync(args, builder =>
{
    // Step dependencies are ordinary services; steps resolve them from ctx.Services.
    builder.Services.AddSingleton<FakeStoreApi>();

    builder.Services.AddPacer()
        .AddScenario(SampleScenarios.Checkout())
        .AddScenario(SampleScenarios.Search())
        .AddScenario(SampleScenarios.Orders())
        // A custom report format, written alongside the built-in console, CSV and HTML reports.
        .AddReportWriter<JsonReportWriter>();
});
