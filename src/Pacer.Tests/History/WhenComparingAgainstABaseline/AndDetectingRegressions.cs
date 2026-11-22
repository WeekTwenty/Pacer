using Pacer.History;

namespace Pacer.Tests.History.WhenComparingAgainstABaseline;

public partial class WhenComparingAgainstABaseline
{
    public class AndDetectingRegressions
    {
        [Test]
        public void ThenItFlagsALatencyRegression()
        {
            var baseline = HistoryFixtures.Record("checkout", HistoryFixtures.Step("login", meanMs: 100, p99Ms: 150));
            var current = HistoryFixtures.Record("checkout", HistoryFixtures.Step("login", meanMs: 200, p99Ms: 300));

            var regressions = Regressions(current, baseline);

            Assert.That(regressions.Any(r => r.StepName == "login" && r.Metric == "p99_ms"), Is.True);
        }

        [Test]
        public void ThenItDoesNotFlagAWithinToleranceChange()
        {
            var baseline = HistoryFixtures.Record("checkout", HistoryFixtures.Step("login", meanMs: 100, p99Ms: 150));
            var current = HistoryFixtures.Record("checkout", HistoryFixtures.Step("login", meanMs: 110, p99Ms: 160));

            Assert.That(Regressions(current, baseline), Is.Empty);
        }

        [Test]
        public void ThenItFlagsAThroughputDrop()
        {
            var baseline = HistoryFixtures.Record("checkout", HistoryFixtures.Step("login", rps: 100));
            var current = HistoryFixtures.Record("checkout", HistoryFixtures.Step("login", rps: 50));

            Assert.That(Regressions(current, baseline).Any(r => r.Metric == "rps"), Is.True);
        }

        [Test]
        public void ThenItFlagsAnErrorRateIncrease()
        {
            var baseline = HistoryFixtures.Record("checkout", HistoryFixtures.Step("login", ok: 100, fail: 1));
            var current = HistoryFixtures.Record("checkout", HistoryFixtures.Step("login", ok: 50, fail: 50));

            Assert.That(Regressions(current, baseline).Any(r => r.Metric == "fail_rate"), Is.True);
        }

        [Test]
        public void ThenItReturnsEveryComparableMetric()
        {
            var baseline = HistoryFixtures.Record("checkout", HistoryFixtures.Step("login", meanMs: 100));
            var current = HistoryFixtures.Record("checkout", HistoryFixtures.Step("login", meanMs: 100));

            var comparisons = BaselineComparer.Compare(current, baseline);

            Assert.That(comparisons, Is.Not.Empty);
            Assert.That(comparisons.All(c => !c.Regressed), Is.True);
        }

        private static IReadOnlyList<MetricComparison> Regressions(RunRecord current, RunRecord baseline, double tolerance = 20)
            => BaselineComparer.Compare(current, baseline, tolerance).Where(c => c.Regressed).ToArray();
    }
}
