using Pacer.History;
using Pacer.Reporting;

namespace Pacer.Tests.Reporting.WhenRenderingTheBaselineComparison;

public partial class WhenRenderingTheBaselineComparison
{
    public class AndRenderingAComparison
    {
        [Test]
        public void ThenItShowsTheBaselineNameAndTolerance()
        {
            var table = BaselineComparisonRenderer.Render("before", [Comparison()], 20);

            Assert.That(table, Does.Contain("Compared against baseline 'before'"));
            Assert.That(table, Does.Contain("20%"));
        }

        [Test]
        public void ThenItShowsNoRegressionsWhenNone()
        {
            var table = BaselineComparisonRenderer.Render("before", [Comparison()], 20);

            Assert.That(table, Does.Contain("No regressions detected."));
        }

        [Test]
        public void ThenItMarksRegressedMetrics()
        {
            var table = BaselineComparisonRenderer.Render("before", [Comparison(regressed: true)], 20);

            Assert.That(table, Does.Contain("✗"));
            Assert.That(table, Does.Contain("1 regression detected."));
        }

        private static MetricComparison Comparison(bool regressed = false) => new(
            "login", "p99_ms", 20.0, 24.0, 20.0, regressed);
    }
}
