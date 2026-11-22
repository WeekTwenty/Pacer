using Pacer.History;

namespace Pacer.Tests.History.WhenComparingAgainstABaseline;

public partial class WhenComparingAgainstABaseline
{
    public class AndBuildingAComparison
    {
        [Test]
        public void ThenItRecordsTheBaselineNameAndTolerance()
        {
            var record = HistoryFixtures.Record("checkout", HistoryFixtures.Step("login"));

            var comparison = BaselineComparison.Create(record, record, "v1.3.0", tolerancePercent: 15);

            Assert.That(comparison.BaselineName, Is.EqualTo("v1.3.0"));
            Assert.That(comparison.TolerancePercent, Is.EqualTo(15));
        }

        [Test]
        public void ThenItIsLikeForLikeWhenTheLoadMatches()
        {
            var record = HistoryFixtures.Record("checkout", HistoryFixtures.Step("login"));

            var comparison = BaselineComparison.Create(record, record, "v1.3.0", 20);

            Assert.That(comparison.IsLikeForLike, Is.True);
        }

        [Test]
        public void ThenItIsNotLikeForLikeWhenThePeakUsersDiffer()
        {
            var baseline = HistoryFixtures.Record("checkout", HistoryFixtures.Step("login"));
            var current = baseline with { PeakUsers = baseline.PeakUsers + 10 };

            var comparison = BaselineComparison.Create(current, baseline, "v1.3.0", 20);

            Assert.That(comparison.IsLikeForLike, Is.False);
            Assert.That(comparison.BaselinePeakUsers, Is.EqualTo(baseline.PeakUsers));
        }

        [Test]
        public void ThenItCountsRegressions()
        {
            var baseline = HistoryFixtures.Record("checkout", HistoryFixtures.Step("login", meanMs: 100, p99Ms: 150));
            var current = HistoryFixtures.Record("checkout", HistoryFixtures.Step("login", meanMs: 200, p99Ms: 300));

            var comparison = BaselineComparison.Create(current, baseline, "v1.3.0", 20);

            Assert.That(comparison.HasRegressions, Is.True);
            Assert.That(comparison.RegressionCount, Is.EqualTo(comparison.Metrics.Count(m => m.Regressed)));
        }
    }
}
