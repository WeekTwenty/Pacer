using Pacer.Hosting;

namespace Pacer.Tests.Hosting.WhenParsingRunArguments;

public partial class WhenParsingRunArguments
{
    public class AndBindingTheOptions
    {
        private static RunOptions Bind(params string[] args)
        {
            var cli = new PacerCommandLine(
                (_, _) => Task.FromResult(0),
                _ => Task.FromResult(0),
                (_, _) => Task.FromResult(0),
                (_, _) => Task.FromResult(0));
            var parseResult = cli.Root.Parse(args);
            return cli.BindRun(parseResult);
        }

        [Test]
        public void ThenItBindsTheScenarioName()
        {
            var options = Bind("run", "--scenario", "checkout");

            Assert.That(options.Scenario, Is.EqualTo("checkout"));
        }

        [Test]
        public void ThenItBindsTheUserCount()
        {
            var options = Bind("run", "--users", "150");

            Assert.That(options.Users, Is.EqualTo(150));
        }

        [Test]
        public void ThenItParsesTheDurationAsMinutes()
        {
            var options = Bind("run", "--duration", "2");

            Assert.That(options.Duration, Is.EqualTo(TimeSpan.FromMinutes(2)));
        }

        [Test]
        public void ThenItParsesADurationWithAUnit()
        {
            var options = Bind("run", "--duration", "45s");

            Assert.That(options.Duration, Is.EqualTo(TimeSpan.FromSeconds(45)));
        }

        [Test]
        public void ThenItParsesTheWarmup()
        {
            var options = Bind("run", "--warmup", "10s");

            Assert.That(options.Warmup, Is.EqualTo(TimeSpan.FromSeconds(10)));
        }

        [Test]
        public void ThenUnsetDurationBindsToNull()
        {
            var options = Bind("run", "--scenario", "checkout");

            Assert.That(options.Duration, Is.Null);
        }

        [Test]
        public void ThenAnInvalidDurationIsAParseError()
        {
            var cli = new PacerCommandLine(
                (_, _) => Task.FromResult(0),
                _ => Task.FromResult(0),
                (_, _) => Task.FromResult(0),
                (_, _) => Task.FromResult(0));

            var parseResult = cli.Root.Parse(["run", "--duration", "soon"]);

            Assert.That(parseResult.Errors, Is.Not.Empty);
        }

        [Test]
        public void ThenItBindsTheProfile()
        {
            var options = Bind("run", "--profile", "spike");

            Assert.That(options.Profile, Is.EqualTo("spike"));
        }

        [Test]
        public void ThenItBindsTheAllFlag()
        {
            var options = Bind("run", "--all");

            Assert.That(options.All, Is.True);
        }

        [Test]
        public void ThenUnsetUsersBindsToNull()
        {
            var options = Bind("run", "--scenario", "checkout");

            Assert.That(options.Users, Is.Null);
        }
    }
}