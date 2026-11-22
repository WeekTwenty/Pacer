using Pacer.Scenarios;

namespace Pacer.Tests.Scenarios.WhenBuildingAScenario;

public partial class WhenBuildingAScenario
{
    public class AndTheConfigurationIsInvalid : ScenarioTestBase
    {
        [Test]
        public void ThenItThrowsWhenNoStepsAreDefined()
        {
            var scenario = Scenario.Create("empty");

            Assert.That(() => scenario.Build(), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void ThenItThrowsWhenAStepNameIsDuplicated()
        {
            var scenario = Scenario.Create("dupes").AddStep("login", AnyStep());

            Assert.That(() => scenario.AddStep("login", AnyStep()), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void ThenItThrowsWhenTheNameIsBlank()
        {
            Assert.That(() => Scenario.Create("  "), Throws.TypeOf<ArgumentException>());
        }
    }
}