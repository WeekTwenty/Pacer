using Pacer.Reporting;

namespace Pacer.Tests.Reporting.WhenRenderingCsv;

public partial class WhenRenderingCsv
{
    public class AndRenderingTheSummary
    {
        [Test]
        public void ThenItStartsWithTheHeaderRow()
        {
            var csv = CsvReportWriter.RenderSummary([ReportFixtures.Report()]);

            var firstLine = csv.Split('\n')[0].Trim();
            Assert.That(firstLine, Is.EqualTo("scenario,row,ok,fail,total,rps,min_us,mean_us,p50_us,p75_us,p95_us,p99_us,p999_us,max_us,bytes_sent,bytes_received"));
        }

        [Test]
        public void ThenItWritesARowForEachStep()
        {
            var csv = CsvReportWriter.RenderSummary([ReportFixtures.Report()]);

            Assert.That(csv, Does.Contain("checkout,login,100,5,105,"));
        }

        [Test]
        public void ThenItWritesAJourneyTotalRow()
        {
            var csv = CsvReportWriter.RenderSummary([ReportFixtures.Report()]);

            Assert.That(csv, Does.Contain("checkout,checkout,95,5,100,"));
        }

        [Test]
        public void ThenItQuotesFieldsContainingAComma()
        {
            var report = ReportFixtures.Report(steps: new[] { ReportFixtures.Step("a,b") });

            var csv = CsvReportWriter.RenderSummary([report]);

            Assert.That(csv, Does.Contain("\"a,b\""));
        }

        [Test]
        public void ThenItWritesLatencyAsWholeMicroseconds()
        {
            var csv = CsvReportWriter.RenderSummary([ReportFixtures.Report()]);

            // MeanMs 5.25 → 5250 µs; MinMs 1.0 → 1000 µs.
            Assert.That(csv, Does.Contain("5250").And.Contain("1000"));
        }
    }
}