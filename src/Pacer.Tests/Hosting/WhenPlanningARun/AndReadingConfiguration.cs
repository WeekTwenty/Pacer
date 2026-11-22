using Pacer.Hosting;
using Pacer.Load;

namespace Pacer.Tests.Hosting.WhenPlanningARun;

public partial class WhenPlanningARun
{
    public class AndReadingConfiguration : PlannerTestBase
    {
        private static PacerSettings Settings(LoadSettings load)
        {
            return new PacerSettings { Load = load };
        }

        private static LoadSettings Ramp()
        {
            return new LoadSettings { Profile = "ramp", Users = 50, RampUp = "10s", Hold = "20s", RampDown = "10s" };
        }

        [Test]
        public void ThenTheConfiguredLoadAppliesToEveryScenario()
        {
            var settings = Settings(new LoadSettings { Profile = "soak", Users = 30, Duration = "2m" });

            var result = RunPlanner.Apply(Scenario("search"), new RunOptions(), settings);

            Assert.That(result.Load.Kind, Is.EqualTo("Soak"));
            Assert.That(result.Load.PeakUsers, Is.EqualTo(30));
            Assert.That(result.Load.TotalDuration, Is.EqualTo(TimeSpan.FromMinutes(2)));
        }

        [Test]
        public void ThenExplicitRampPhasesAreUsed()
        {
            var result = RunPlanner.Apply(Scenario(), new RunOptions(), Settings(Ramp()));

            Assert.That(result.Load.Kind, Is.EqualTo("Ramp"));
            Assert.That(Stage(result.Load, 0).Duration, Is.EqualTo(TimeSpan.FromSeconds(10)));
            Assert.That(Stage(result.Load, 1).Duration, Is.EqualTo(TimeSpan.FromSeconds(20)));
            Assert.That(result.Load.TotalDuration, Is.EqualTo(TimeSpan.FromSeconds(40)));
        }

        [Test]
        public void ThenUnsetFieldsFallBackToTheDefaults()
        {
            var result = RunPlanner.Apply(Scenario(), new RunOptions(), Settings(new LoadSettings { Users = 25 }));

            Assert.That(result.Load.Kind, Is.EqualTo("Load"));
            Assert.That(result.Load.PeakUsers, Is.EqualTo(25));
            Assert.That(result.Load.TotalDuration, Is.EqualTo(TimeSpan.FromMinutes(1)));
        }

        [Test]
        public void ThenACommandLineUserOverrideKeepsTheConfiguredRampPhases()
        {
            var result = RunPlanner.Apply(Scenario(), new RunOptions { Users = 200 }, Settings(Ramp()));

            Assert.That(result.Load.PeakUsers, Is.EqualTo(200));
            Assert.That(result.Load.TotalDuration, Is.EqualTo(TimeSpan.FromSeconds(40)));
        }

        [Test]
        public void ThenACommandLineDurationOverrideRederivesTheRampPhases()
        {
            var result = RunPlanner.Apply(Scenario(), new RunOptions { Duration = TimeSpan.FromSeconds(90) }, Settings(Ramp()));

            Assert.That(result.Load.TotalDuration, Is.EqualTo(TimeSpan.FromSeconds(90)));
            Assert.That(Stage(result.Load, 0).Duration, Is.EqualTo(TimeSpan.FromSeconds(30)));
        }

        [Test]
        public void ThenACommandLineProfileOverrideReplacesTheConfiguredShape()
        {
            var result = RunPlanner.Apply(Scenario(), new RunOptions { Profile = "load" }, Settings(Ramp()));

            Assert.That(result.Load.Kind, Is.EqualTo("Load"));
            Assert.That(result.Load.PeakUsers, Is.EqualTo(50));
        }

        [Test]
        public void ThenTheConfiguredWarmupOverridesTheCodedWarmup()
        {
            var settings = Settings(new LoadSettings { Warmup = "5s" });

            var result = RunPlanner.Apply(Scenario(warmup: TimeSpan.FromSeconds(2)), new RunOptions(), settings);

            Assert.That(result.Warmup, Is.EqualTo(TimeSpan.FromSeconds(5)));
        }

        [Test]
        public void ThenACustomProfileUsesItsStages()
        {
            var settings = Settings(new LoadSettings
            {
                Profile = "custom",
                Stages =
                [
                    new LoadStageSettings { StartUsers = 0, EndUsers = 20, Duration = "10s" },
                    new LoadStageSettings { StartUsers = 20, EndUsers = 20, Duration = "30s" },
                ],
            });

            var result = RunPlanner.Apply(Scenario(), new RunOptions(), settings);

            Assert.That(result.Load.Kind, Is.EqualTo("Custom"));
            Assert.That(result.Load.PeakUsers, Is.EqualTo(20));
            Assert.That(result.Load.TotalDuration, Is.EqualTo(TimeSpan.FromSeconds(40)));
        }

        [Test]
        public void ThenACustomProfileWithoutStagesThrows()
        {
            var settings = Settings(new LoadSettings { Profile = "custom" });

            Assert.That(
                () => RunPlanner.Apply(Scenario(), new RunOptions(), settings),
                Throws.TypeOf<LoadConfigurationException>().With.Message.Contains("Stages"));
        }

        [Test]
        public void ThenAnInvalidDurationThrowsNamingTheField()
        {
            var settings = Settings(new LoadSettings { Duration = "soon" });

            Assert.That(
                () => RunPlanner.Apply(Scenario(), new RunOptions(), settings),
                Throws.TypeOf<LoadConfigurationException>().With.Message.Contains("Duration").And.Message.Contains("checkout"));
        }
    }
}
