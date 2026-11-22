using Pacer.Reporting;

namespace Pacer.Tests.Reporting.WhenRenderingCsv;

public partial class WhenRenderingCsv
{
    public class AndRenderingTheBaselineComparison
    {
        private static string[] Lines(params Pacer.Metrics.RunReport[] reports)
        {
            return CsvReportWriter.RenderBaseline(reports).Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        }

        [Test]
        public void ThenItWritesAHeader()
        {
            var lines = Lines(ReportFixtures.Report() with { Baseline = ReportFixtures.Comparison() });

            Assert.That(lines[0], Is.EqualTo("scenario,baseline,step,metric,baseline_value,current_value,percent_change,regressed"));
        }

        [Test]
        public void ThenItWritesOneRowPerMetric()
        {
            var lines = Lines(ReportFixtures.Report() with { Baseline = ReportFixtures.Comparison() });

            Assert.That(lines, Has.Length.EqualTo(4));
            Assert.That(lines[2], Is.EqualTo("checkout,v1.3.0,login,p95_ms,10,10.5,5,false"));
        }

        [Test]
        public void ThenItFlagsRegressedMetrics()
        {
            var lines = Lines(ReportFixtures.Report() with { Baseline = ReportFixtures.Comparison(regressed: true) });

            Assert.That(lines[2], Does.EndWith(",true"));
        }

        [Test]
        public void ThenItSkipsReportsThatWereNotCompared()
        {
            var lines = Lines(ReportFixtures.Report());

            Assert.That(lines, Has.Length.EqualTo(1));
        }

        [Test]
        public void ThenItQuotesNamesContainingCommas()
        {
            var lines = Lines(ReportFixtures.Report() with { Baseline = ReportFixtures.Comparison(name: "v1,3") });

            Assert.That(lines[1], Does.StartWith("checkout,\"v1,3\","));
        }
    }
}
