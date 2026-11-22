using Pacer.Hosting;
using Pacer.Load;

namespace Pacer.Tests.Hosting.WhenPlanningARun;

public partial class WhenPlanningARun
{
    public class AndApplyingOverrides : PlannerTestBase
    {
        [Test]
        public void ThenItUsesTheBuiltInDefaultWhenNothingIsConfigured()
        {
            var result = RunPlanner.Apply(Scenario(), new RunOptions());

            Assert.That(result.Load.Kind, Is.EqualTo("Load"));
            Assert.That(result.Load.PeakUsers, Is.EqualTo(10));
            Assert.That(result.Load.TotalDuration, Is.EqualTo(TimeSpan.FromMinutes(1)));
        }

        [Test]
        public void ThenOverridingUsersChangesThePeak()
        {
            var result = RunPlanner.Apply(Scenario(), new RunOptions { Users = 200 });

            Assert.That(result.Load.PeakUsers, Is.EqualTo(200));
        }

        [Test]
        public void ThenOverridingTheProfileChangesTheKind()
        {
            var result = RunPlanner.Apply(Scenario(), new RunOptions { Profile = "ramp", Users = 100, Duration = TimeSpan.FromSeconds(60) });

            Assert.That(result.Load.Kind, Is.EqualTo("Ramp"));
        }

        [Test]
        public void ThenOverridingTheDurationChangesTheTotal()
        {
            var result = RunPlanner.Apply(Scenario(), new RunOptions { Duration = TimeSpan.FromSeconds(90) });

            Assert.That(result.Load.TotalDuration, Is.EqualTo(TimeSpan.FromSeconds(90)));
        }

        [Test]
        public void ThenAnUnknownProfileThrows()
        {
            Assert.That(
                () => RunPlanner.Apply(Scenario(), new RunOptions { Profile = "rocket" }),
                Throws.TypeOf<LoadConfigurationException>().With.Message.Contains("rocket"));
        }

        [Test]
        public void ThenOverridingTheWarmupIsApplied()
        {
            var result = RunPlanner.Apply(Scenario(), new RunOptions { Warmup = TimeSpan.FromSeconds(5) });

            Assert.That(result.Warmup, Is.EqualTo(TimeSpan.FromSeconds(5)));
        }

        [Test]
        public void ThenTheCodedWarmupIsKeptWhenNothingOverridesIt()
        {
            var result = RunPlanner.Apply(Scenario(warmup: TimeSpan.FromSeconds(2)), new RunOptions());

            Assert.That(result.Warmup, Is.EqualTo(TimeSpan.FromSeconds(2)));
        }
    }
}
