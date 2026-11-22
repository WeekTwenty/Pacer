using Pacer.Load;

namespace Pacer.Tests.Load.WhenParsingDurations;

public partial class WhenParsingDurations
{
    public class AndParsingADuration
    {
        [TestCase("500ms", 0, 0, 0, 500)]
        [TestCase("30s", 0, 0, 30, 0)]
        [TestCase("5m", 0, 5, 0, 0)]
        [TestCase("1h30m", 1, 30, 0, 0)]
        [TestCase("1m30s", 0, 1, 30, 0)]
        [TestCase("00:02:15", 0, 2, 15, 0)]
        [TestCase("2", 0, 2, 0, 0)]
        [TestCase("1.5", 0, 1, 30, 0)]
        [TestCase(" 10S ", 0, 0, 10, 0)]
        public void ThenItParsesTheValue(string value, int hours, int minutes, int seconds, int milliseconds)
        {
            Assert.That(Durations.Parse(value), Is.EqualTo(new TimeSpan(0, hours, minutes, seconds, milliseconds)));
        }

        [TestCase("")]
        [TestCase("soon")]
        [TestCase("10x")]
        [TestCase("-5")]
        [TestCase("5m soon")]
        public void ThenItRejectsInvalidValues(string value)
        {
            Assert.That(Durations.TryParse(value, out _), Is.False);
        }

        [Test]
        public void ThenParseThrowsAFormatExceptionForInvalidValues()
        {
            Assert.That(() => Durations.Parse("soon"), Throws.TypeOf<FormatException>());
        }

        [TestCase(90, "1m30s")]
        [TestCase(3600, "1h")]
        [TestCase(0, "0s")]
        public void ThenItFormatsCompactly(int seconds, string expected)
        {
            Assert.That(Durations.Format(TimeSpan.FromSeconds(seconds)), Is.EqualTo(expected));
        }
    }
}
