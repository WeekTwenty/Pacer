namespace Pacer.Load;

/// <summary>
/// A load profile as written in configuration (the <c>Pacer:Load</c> section). Every field is optional:
/// unset fields fall back to Pacer's built-in defaults, and the <c>run</c> command's flags override them.
/// Durations accept <c>30s</c>, <c>5m</c>, <c>1h30m</c> or <c>00:05:00</c>; a bare number is minutes.
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
public sealed class LoadSettings
{
    /// <summary>The profile shape: <c>load</c>, <c>soak</c>, <c>spike</c>, <c>stress</c>, <c>ramp</c> or <c>custom</c>.</summary>
    public string? Profile { get; set; }

    /// <summary>The peak number of virtual users.</summary>
    public int? Users { get; set; }

    /// <summary>The total duration of the profile.</summary>
    public string? Duration { get; set; }

    /// <summary>How long to run un-recorded warm-up journeys before measurement begins.</summary>
    public string? Warmup { get; set; }

    /// <summary>Ramp: time to climb from zero to the peak. Defaults to a third of <see cref="Duration"/>.</summary>
    public string? RampUp { get; set; }

    /// <summary>Ramp: time to hold at the peak. Defaults to whatever is left of <see cref="Duration"/>.</summary>
    public string? Hold { get; set; }

    /// <summary>Ramp: time to fall from the peak back to zero. Defaults to a third of <see cref="Duration"/>.</summary>
    public string? RampDown { get; set; }

    /// <summary>Spike: users before and after the spike. Defaults to a quarter of <see cref="Users"/>.</summary>
    public int? BaselineUsers { get; set; }

    /// <summary>Spike: how long the spike lasts. Defaults to a fifth of <see cref="Duration"/>.</summary>
    public string? SpikeDuration { get; set; }

    /// <summary>Stress: users on the first step. Defaults to a fifth of <see cref="Users"/>.</summary>
    public int? StartUsers { get; set; }

    /// <summary>Stress: users added on each step. Defaults to a fifth of <see cref="Users"/>.</summary>
    public int? StepUsers { get; set; }

    /// <summary>Stress: how long each step is held. Defaults to a fifth of <see cref="Duration"/>.</summary>
    public string? StepDuration { get; set; }

    /// <summary>Custom: explicit stages, used when <see cref="Profile"/> is <c>custom</c>.</summary>
    public List<LoadStageSettings>? Stages { get; set; }
}

/// <summary>One stage of a <c>custom</c> profile in configuration.</summary>
public sealed class LoadStageSettings
{
    /// <summary>Active virtual users at the start of the stage.</summary>
    public int StartUsers { get; set; }

    /// <summary>Active virtual users at the end of the stage.</summary>
    public int EndUsers { get; set; }

    /// <summary>How long the stage lasts.</summary>
    public string? Duration { get; set; }
}
