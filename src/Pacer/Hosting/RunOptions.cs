namespace Pacer.Hosting;

/// <summary>
/// The resolved command-line options for a <c>run</c> invocation. Load fields left unset fall back to
/// configuration (see <see cref="RunPlanner"/>).
/// </summary>
public sealed record RunOptions
{
    /// <summary>The single scenario to run, if any.</summary>
    public string? Scenario { get; init; }

    /// <summary>The scenario group to run, if any.</summary>
    public string? Group { get; init; }

    /// <summary>Whether to run every registered scenario.</summary>
    public bool All { get; init; }

    /// <summary>Override for the peak number of virtual users, or <see langword="null"/> to use the configured value.</summary>
    public int? Users { get; init; }

    /// <summary>Override for the test duration, or <see langword="null"/> to use the configured value.</summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>Override for the load profile shape (load, soak, spike, stress, ramp), or <see langword="null"/> to use the configured shape.</summary>
    public string? Profile { get; init; }

    /// <summary>Override for the warm-up duration, or <see langword="null"/> to use the configured (or coded) value.</summary>
    public TimeSpan? Warmup { get; init; }

    /// <summary>The directory file-based reports are written to.</summary>
    public string OutputDirectory { get; init; } = "reports";

    /// <summary>Directory for Pacer's baseline and history data.</summary>
    public string DataDirectory { get; init; } = ".pacer";

    /// <summary>The baseline name to save the run under, or <see langword="null"/> to skip saving.</summary>
    public string? SaveBaseline { get; init; }

    /// <summary>The baseline name to compare against, or <see langword="null"/> to skip comparison.</summary>
    public string? CompareBaseline { get; init; }

    /// <summary>Whether a detected baseline regression fails the run with a non-zero exit code.</summary>
    public bool FailOnRegression { get; init; }

    /// <summary>Percent change beyond which a baseline comparison counts as a regression.</summary>
    public double BaselineTolerancePercent { get; init; } = 20.0;
}
