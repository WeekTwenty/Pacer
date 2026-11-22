using Pacer.Scenarios;

namespace Pacer.Tests.Scenarios.WhenBuildingAScenario;

public partial class WhenBuildingAScenario
{
    public class AndTheConfigurationIsValid : ScenarioTestBase
    {
        [Test]
        public void ThenItPreservesTheStepOrder()
        {
            var definition = Scenario.Create("checkout")
                .AddStep("login", AnyStep())
                .AddStep("browse", AnyStep())
                .AddStep("purchase", AnyStep())
                .Build();

            var stepNames = definition.Steps.Select(s => s.Name).ToArray();

            Assert.That(stepNames, Is.EqualTo(new[] { "login", "browse", "purchase" }));
        }

        [Test]
        public void ThenItRecordsTheGroup()
        {
            var definition = Scenario.Create("checkout")
                .InGroup("ecommerce")
                .AddStep("login", AnyStep())
                .Build();

            Assert.That(definition.Group, Is.EqualTo("ecommerce"));
        }

        [Test]
        public void ThenItDefaultsWarmupToZero()
        {
            var definition = Scenario.Create("checkout")
                .AddStep("login", AnyStep())
                .Build();

            Assert.That(definition.Warmup, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void ThenItDefaultsTheLoadProfileUntilConfigurationIsApplied()
        {
            var definition = Scenario.Create("checkout")
                .AddStep("login", AnyStep())
                .Build();

            Assert.That(definition.Load.Kind, Is.EqualTo("Load"));
            Assert.That(definition.Load.PeakUsers, Is.EqualTo(10));
            Assert.That(definition.Load.TotalDuration, Is.EqualTo(TimeSpan.FromMinutes(1)));
        }
    }
}