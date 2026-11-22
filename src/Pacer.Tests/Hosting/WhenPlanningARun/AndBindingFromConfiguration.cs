using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pacer.Hosting;

namespace Pacer.Tests.Hosting.WhenPlanningARun;

public partial class WhenPlanningARun
{
    public class AndBindingFromConfiguration : PlannerTestBase
    {
        private static PacerSettings Bind(Dictionary<string, string?> values)
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
            var services = new ServiceCollection().AddSingleton<IConfiguration>(configuration);
            services.AddPacer();
            return services.BuildServiceProvider().GetRequiredService<IOptions<PacerSettings>>().Value;
        }

        [Test]
        public void ThenItBindsTheLoad()
        {
            var settings = Bind(new()
            {
                ["Pacer:Load:Profile"] = "spike",
                ["Pacer:Load:Users"] = "80",
                ["Pacer:Load:Duration"] = "2m",
            });

            var result = RunPlanner.Apply(Scenario(), new RunOptions(), settings);

            Assert.That(result.Load.Kind, Is.EqualTo("Spike"));
            Assert.That(result.Load.PeakUsers, Is.EqualTo(80));
            Assert.That(result.Load.TotalDuration, Is.EqualTo(TimeSpan.FromMinutes(2)));
        }

        [Test]
        public void ThenItBindsCustomStages()
        {
            var settings = Bind(new()
            {
                ["Pacer:Load:Profile"] = "custom",
                ["Pacer:Load:Stages:0:StartUsers"] = "0",
                ["Pacer:Load:Stages:0:EndUsers"] = "15",
                ["Pacer:Load:Stages:0:Duration"] = "20s",
            });

            var result = RunPlanner.Apply(Scenario(), new RunOptions(), settings);

            Assert.That(result.Load.PeakUsers, Is.EqualTo(15));
            Assert.That(result.Load.TotalDuration, Is.EqualTo(TimeSpan.FromSeconds(20)));
        }
    }
}
