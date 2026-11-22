using Pacer.Reporting;

namespace Pacer.Tests.Reporting.WhenRenderingTheConsoleTable;

public partial class WhenRenderingTheConsoleTable
{
    public class AndRenderingMicroseconds
    {
        [Test]
        public void ThenItRendersSubFiveMillisecondLatencyInMicroseconds()
        {
            var report = ReportFixtures.Report(steps: new[] { ReportFixtures.FastStep() });

            var table = ConsoleReportWriter.Render(report);

            Assert.That(table, Does.Contain("2500 µs"));
        }

        [Test]
        public void ThenItRendersLatencyAtOrAboveFiveMillisecondsInMilliseconds()
        {
            var table = ConsoleReportWriter.Render(ReportFixtures.Report());

            Assert.That(table, Does.Contain("5.25 ms"));
        }
    }
}
