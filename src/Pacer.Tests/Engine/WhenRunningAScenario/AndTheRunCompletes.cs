using Pacer.Load;
using Pacer.Scenarios;
using Pacer.Steps;

namespace Pacer.Tests.Engine.WhenRunningAScenario;

public partial class WhenRunningAScenario
{
    public class AndTheRunCompletes : ScenarioRunnerTestBase
    {
        [Test]
        public async Task ThenTheReportCarriesTheScenarioName()
        {
            var scenario = TwoStepScenario(LoadProfiles.Load(2, TimeSpan.FromSeconds(10)));

            var report = await RunToCompletionAsync(scenario);

            Assert.That(report.ScenarioName, Is.EqualTo("checkout"));
        }

        [Test]
        public async Task ThenTheReportHasOneRowPerStep()
        {
            var scenario = TwoStepScenario(LoadProfiles.Load(2, TimeSpan.FromSeconds(10)));

            var report = await RunToCompletionAsync(scenario);

            var names = report.Steps.Select(s => s.Name).ToArray();
            Assert.That(names, Is.EqualTo(new[] { "login", "purchase" }));
        }

        [Test]
        public async Task ThenTheReportDurationMatchesTheLoadProfile()
        {
            var scenario = TwoStepScenario(LoadProfiles.Load(2, TimeSpan.FromSeconds(10)));

            var report = await RunToCompletionAsync(scenario);

            Assert.That(report.Duration, Is.EqualTo(TimeSpan.FromSeconds(10)));
        }

        [Test]
        public async Task ThenItExecutesAtLeastOneJourney()
        {
            var executed = 0;
            var scenario = TwoStepScenario(LoadProfiles.Load(2, TimeSpan.FromSeconds(10)), onStep: () => Interlocked.Increment(ref executed));

            await RunToCompletionAsync(scenario);

            Assert.That(executed, Is.GreaterThan(0));
        }

        [Test]
        public async Task ThenItRecordsSuccessfulRequestsInTheReport()
        {
            var scenario = TwoStepScenario(LoadProfiles.Load(2, TimeSpan.FromSeconds(10)));

            var report = await RunToCompletionAsync(scenario);

            Assert.That(report.Journey.Ok, Is.GreaterThan(0));
        }

        [Test]
        public async Task ThenItCapturesTheLoadProfileKind()
        {
            var scenario = TwoStepScenario(LoadProfiles.Load(2, TimeSpan.FromSeconds(10)));

            var report = await RunToCompletionAsync(scenario);

            Assert.That(report.LoadProfileKind, Is.EqualTo("Load"));
        }

        [Test]
        public async Task ThenJourneysStillRunningAtTheEndAreNotCountedAsFailures()
        {
            // The second step never finishes on its own, so every journey is in flight when the run stops.
            var scenario = Scenario.Create("in-flight")
                .AddStep("login", _ => ValueTask.FromResult(StepResult.Ok()))
                .AddStep("wait", async (_, ct) =>
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                    return StepResult.Ok();
                })
                .Build()
                .WithLoad(LoadProfiles.Load(2, TimeSpan.FromSeconds(10)));

            var report = await RunToCompletionAsync(scenario);

            Assert.That(report.Journey.Fail, Is.Zero);
            Assert.That(report.Steps.Single(s => s.Name == "wait").Fail, Is.Zero);
            Assert.That(report.Steps.Single(s => s.Name == "login").Ok, Is.GreaterThanOrEqualTo(2));
        }
    }
}
