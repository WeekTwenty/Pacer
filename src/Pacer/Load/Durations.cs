using System.Globalization;
using System.Text.RegularExpressions;

namespace Pacer.Load;

/// <summary>
/// Parses the human-friendly durations accepted in configuration and on the command line:
/// unit suffixes (<c>500ms</c>, <c>30s</c>, <c>5m</c>, <c>1h30m</c>), a <see cref="TimeSpan"/>
/// (<c>00:05:00</c>), or a bare number, which is read as minutes.
/// </summary>
public static partial class Durations
{
    /// <summary>Parses <paramref name="value"/>, throwing <see cref="FormatException"/> when it is not a valid duration.</summary>
    public static TimeSpan Parse(string value)
        => TryParse(value, out var duration)
            ? duration
            : throw new FormatException($"Invalid duration '{value}'. Use a value such as 30s, 5m, 1h30m or 00:05:00 (a bare number is read as minutes).");

    /// <summary>Tries to parse <paramref name="value"/> as a duration.</summary>
    public static bool TryParse(string? value, out TimeSpan duration)
    {
        duration = TimeSpan.Zero;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var text = value.Trim();

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var minutes))
        {
            if (minutes < 0)
                return false;
            duration = TimeSpan.FromMinutes(minutes);
            return true;
        }

        if (text.Contains(':'))
            return TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out duration) && duration >= TimeSpan.Zero;

        if (!UnitSequence().IsMatch(text))
            return false;

        foreach (Match part in UnitPart().Matches(text))
        {
            var amount = double.Parse(part.Groups["amount"].Value, CultureInfo.InvariantCulture);
            duration += part.Groups["unit"].Value.ToLowerInvariant() switch
            {
                "ms" => TimeSpan.FromMilliseconds(amount),
                "s" => TimeSpan.FromSeconds(amount),
                "m" => TimeSpan.FromMinutes(amount),
                _ => TimeSpan.FromHours(amount),
            };
        }

        return true;
    }

    /// <summary>Formats a duration compactly for logs and listings (e.g. <c>1m30s</c>).</summary>
    public static string Format(TimeSpan duration)
    {
        if (duration == TimeSpan.Zero)
            return "0s";
        if (duration < TimeSpan.FromSeconds(1))
            return $"{duration.TotalMilliseconds:0}ms";

        var parts = new List<string>(3);
        if ((int)duration.TotalHours > 0)
            parts.Add($"{(int)duration.TotalHours}h");
        if (duration.Minutes > 0)
            parts.Add($"{duration.Minutes}m");
        if (duration.Seconds > 0 || duration.Milliseconds > 0)
            parts.Add(duration.Milliseconds > 0 ? $"{duration.TotalSeconds % 60:0.###}s" : $"{duration.Seconds}s");
        return string.Concat(parts);
    }

    [GeneratedRegex(@"^(\d+(\.\d+)?(ms|h|m|s))+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UnitSequence();

    [GeneratedRegex(@"(?<amount>\d+(\.\d+)?)(?<unit>ms|h|m|s)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UnitPart();
}
