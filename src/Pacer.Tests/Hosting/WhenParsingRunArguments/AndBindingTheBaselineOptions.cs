using Pacer.Hosting;

namespace Pacer.Tests.Hosting.WhenParsingRunArguments;

public partial class WhenParsingRunArguments
{
    public class AndBindingTheBaselineOptions
    {
        private static RunOptions Bind(params string[] args)
        {
            var cli = new PacerCommandLine(
                (_, _) => Task.FromResult(0),
                _ => Task.FromResult(0),
                (_, _) => Task.FromResult(0),
                (_, _) => Task.FromResult(0));
            return cli.BindRun(cli.Root.Parse(args));
        }

        [Test]
        public void ThenSaveBaselineDefaultsToNull()
        {
            var options = Bind("run", "--scenario", "checkout");

            Assert.That(options.SaveBaseline, Is.Null);
        }

        [Test]
        public void ThenItBindsTheSaveBaselineFlagToTheDefaultName()
        {
            var options = Bind("run", "--scenario", "checkout", "--save-baseline");

            Assert.That(options.SaveBaseline, Is.EqualTo("default"));
        }

        [Test]
        public void ThenItBindsASaveBaselineName()
        {
            var options = Bind("run", "--scenario", "checkout", "--save-baseline", "release-1.0");

            Assert.That(options.SaveBaseline, Is.EqualTo("release-1.0"));
        }

        [Test]
        public void ThenCompareBaselineDefaultsToNull()
        {
            var options = Bind("run", "--scenario", "checkout");

            Assert.That(options.CompareBaseline, Is.Null);
        }

        [Test]
        public void ThenItBindsTheCompareBaselineFlagToTheDefaultName()
        {
            var options = Bind("run", "--scenario", "checkout", "--baseline");

            Assert.That(options.CompareBaseline, Is.EqualTo("default"));
        }

        [Test]
        public void ThenItBindsACompareBaselineName()
        {
            var options = Bind("run", "--scenario", "checkout", "--baseline", "release-1.0");

            Assert.That(options.CompareBaseline, Is.EqualTo("release-1.0"));
        }

        [Test]
        public void ThenItBindsTheFailOnRegressionFlag()
        {
            var options = Bind("run", "--scenario", "checkout", "--fail-on-regression");

            Assert.That(options.FailOnRegression, Is.True);
        }

        [Test]
        public void ThenItDefaultsTheBaselineTolerance()
        {
            var options = Bind("run", "--scenario", "checkout");

            Assert.That(options.BaselineTolerancePercent, Is.EqualTo(20.0));
        }

        [Test]
        public void ThenItBindsTheDataDirectory()
        {
            var options = Bind("run", "--scenario", "checkout", "--data-dir", ".perf");

            Assert.That(options.DataDirectory, Is.EqualTo(".perf"));
        }

        [Test]
        public void ThenItDefaultsTheDataDirectory()
        {
            var options = Bind("run", "--scenario", "checkout");

            Assert.That(options.DataDirectory, Is.EqualTo(".pacer"));
        }
    }
}
