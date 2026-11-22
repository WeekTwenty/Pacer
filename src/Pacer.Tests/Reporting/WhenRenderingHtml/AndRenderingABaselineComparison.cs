using Pacer.Reporting;

namespace Pacer.Tests.Reporting.WhenRenderingHtml;

public partial class WhenRenderingHtml
{
    public class AndRenderingABaselineComparison
    {
        private static string Render(bool regressed = false, bool likeForLike = true, string name = "v1.3.0")
        {
            var report = ReportFixtures.Report() with { Baseline = ReportFixtures.Comparison(name, regressed, likeForLike) };
            return HtmlReportWriter.Render([report]);
        }

        [Test]
        public void ThenItAddsAComparisonSectionNamingTheBaseline()
        {
            var html = Render();

            Assert.That(html, Does.Contain("vs baseline v1.3.0"));
            Assert.That(html, Does.Contain("<table class=\"cmp\">"));
        }

        [Test]
        public void ThenItShowsEachMetricsBaselineAndCurrentValues()
        {
            var html = Render();

            Assert.That(html, Does.Contain("<td>p95</td><td>10.00 ms</td><td>10.50 ms</td><td>+5.0%</td>"));
        }

        [Test]
        public void ThenItShowsANoRegressionsBadge()
        {
            var html = Render();

            Assert.That(html, Does.Contain("<div class=\"verdict ok\">no regressions vs v1.3.0</div>"));
        }

        [Test]
        public void ThenItMarksRegressedMetrics()
        {
            var html = Render(regressed: true);

            Assert.That(html, Does.Contain("<tr class=\"regressed\">"));
            Assert.That(html, Does.Contain("<div class=\"verdict fail\">1 regression vs v1.3.0</div>"));
        }

        [Test]
        public void ThenItWarnsWhenTheComparisonIsNotLikeForLike()
        {
            var html = Render(likeForLike: false);

            Assert.That(html, Does.Contain("isn't like-for-like"));
        }

        [Test]
        public void ThenItDoesNotWarnForALikeForLikeComparison()
        {
            Assert.That(Render(), Does.Not.Contain("isn't like-for-like"));
        }

        [Test]
        public void ThenItEncodesTheBaselineName()
        {
            var html = Render(name: "<v2>");

            Assert.That(html, Does.Contain("vs baseline &lt;v2&gt;"));
            Assert.That(html, Does.Not.Contain("<v2>"));
        }

        [Test]
        public void ThenItOmitsTheSectionWhenTheRunWasNotCompared()
        {
            var html = HtmlReportWriter.Render([ReportFixtures.Report()]);

            Assert.That(html, Does.Not.Contain("vs baseline"));
            Assert.That(html, Does.Not.Contain("class=\"verdict"));
        }
    }
}
