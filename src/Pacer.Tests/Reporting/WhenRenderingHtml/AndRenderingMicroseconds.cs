using Pacer.Reporting;

namespace Pacer.Tests.Reporting.WhenRenderingHtml;

public partial class WhenRenderingHtml
{
    public class AndRenderingMicroseconds
    {
        [Test]
        public void ThenItRendersSubFiveMillisecondLatencyInMicroseconds()
        {
            var report = ReportFixtures.Report(steps: new[] { ReportFixtures.FastStep() });

            var html = HtmlReportWriter.Render([report]);

            Assert.That(html, Does.Contain("2500 µs"));
        }
    }
}
