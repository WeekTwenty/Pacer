using Pacer.History;
using Pacer.Metrics;

namespace Pacer.Tests.History;

internal static class HistoryFixtures
{
    public static StepStats Step(
        string name,
        double meanMs = 100,
        double p99Ms = 150,
        long ok = 100,
        long fail = 5,
        double rps = 10.5)
        => new()
        {
            Name = name,
            Ok = ok,
            Fail = fail,
            BytesSent = 512,
            BytesReceived = 1536,
            RequestsPerSecond = rps,
            MinMs = 1.0,
            MeanMs = meanMs,
            MaxMs = p99Ms * 1.5,
            P50Ms = meanMs * 0.8,
            P75Ms = meanMs * 0.9,
            P95Ms = p99Ms * 0.9,
            P99Ms = p99Ms,
            P999Ms = p99Ms * 1.1,
        };

    public static RunRecord Record(string scenarioName = "checkout", params StepStats[] steps)
    {
        var stepList = steps.Length > 0 ? steps : [Step("login"), Step("purchase")];
        return new RunRecord
        {
            ScenarioName = scenarioName,
            Group = "ecommerce",
            LoadProfileKind = "Ramp",
            PeakUsers = 200,
            StartedAt = new DateTimeOffset(2026, 6, 13, 10, 0, 0, TimeSpan.Zero),
            Duration = TimeSpan.FromSeconds(60),
            Environment = new EnvironmentInfo("TestOS", ".NET 10.0", 8, "test-machine"),
            Steps = stepList,
            Journey = Step(scenarioName, meanMs: 120, p99Ms: 180, ok: 95, fail: 5),
        };
    }
}
