using Pacer.Load;

namespace Pacer.Hosting;

/// <summary>
/// Pacer's configuration, bound from the <c>Pacer</c> section (<c>appsettings.json</c>,
/// <c>appsettings.{Environment}.json</c>, environment variables such as <c>Pacer__Load__Users=50</c>,
/// or any other configuration source). Command-line flags on <c>run</c> override it.
/// </summary>
/// <example>
/// <code><![CDATA[
/// {
///   "Pacer": {
///     "Load": { "Profile": "ramp", "Users": 50, "RampUp": "10s", "Hold": "20s", "RampDown": "10s" }
///   }
/// }
/// ]]></code>
/// </example>
public sealed class PacerSettings
{
    /// <summary>The configuration section Pacer binds from.</summary>
    public const string SectionName = "Pacer";

    /// <summary>The load profile every scenario runs with.</summary>
    public LoadSettings Load { get; set; } = new();
}
