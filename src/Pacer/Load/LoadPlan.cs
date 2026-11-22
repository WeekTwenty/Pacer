namespace Pacer.Load;

/// <summary>
/// A parsed, layerable description of a load profile. Layers (built-in defaults, <c>Pacer:Load</c>,
/// command-line flags) are combined with <see cref="Overlay"/> and the
/// result turned into a <see cref="LoadProfile"/> with <see cref="Build"/>.
/// </summary>
internal sealed record LoadPlan
{
    /// <summary>What a scenario runs with when nothing is configured: 10 constant users for a minute.</summary>
    public static readonly LoadPlan Defaults = new() { Profile = "load", Users = 10, Duration = TimeSpan.FromMinutes(1) };

    public string? Profile { get; init; }
    public int? Users { get; init; }
    public TimeSpan? Duration { get; init; }
    public TimeSpan? Warmup { get; init; }
    public TimeSpan? RampUp { get; init; }
    public TimeSpan? Hold { get; init; }
    public TimeSpan? RampDown { get; init; }
    public int? BaselineUsers { get; init; }
    public TimeSpan? SpikeDuration { get; init; }
    public int? StartUsers { get; init; }
    public int? StepUsers { get; init; }
    public TimeSpan? StepDuration { get; init; }
    public IReadOnlyList<LoadStage>? Stages { get; init; }

    /// <summary>Parses a configuration section's settings, or returns <see langword="null"/> for none.</summary>
    public static LoadPlan? From(LoadSettings? settings)
    {
        if (settings is null)
            return null;

        return new LoadPlan
        {
            Profile = Blank(settings.Profile),
            Users = settings.Users,
            Duration = Parse(settings.Duration, nameof(LoadSettings.Duration)),
            Warmup = Parse(settings.Warmup, nameof(LoadSettings.Warmup)),
            RampUp = Parse(settings.RampUp, nameof(LoadSettings.RampUp)),
            Hold = Parse(settings.Hold, nameof(LoadSettings.Hold)),
            RampDown = Parse(settings.RampDown, nameof(LoadSettings.RampDown)),
            BaselineUsers = settings.BaselineUsers,
            SpikeDuration = Parse(settings.SpikeDuration, nameof(LoadSettings.SpikeDuration)),
            StartUsers = settings.StartUsers,
            StepUsers = settings.StepUsers,
            StepDuration = Parse(settings.StepDuration, nameof(LoadSettings.StepDuration)),
            Stages = settings.Stages is { Count: > 0 } stages
                ? stages.Select((s, i) => new LoadStage(s.StartUsers, s.EndUsers, Parse(s.Duration, $"Stages[{i}].Duration") ?? TimeSpan.Zero)).ToArray()
                : null,
        };
    }

    /// <summary>
    /// Lays <paramref name="over"/> on top of this plan. Fields it sets win. Changing the profile discards
    /// every shape detail below it; changing the users discards user-derived details (spike baseline,
    /// stress start/step); changing the duration discards time-derived details (ramp phases, spike and
    /// stress timings). Discarded details are re-derived from the users and duration when built.
    /// </summary>
    public LoadPlan Overlay(LoadPlan? over)
    {
        if (over is null)
            return this;

        var profileChanged = over.Profile is not null && !string.Equals(over.Profile, Profile, StringComparison.OrdinalIgnoreCase);
        var keepUserShape = !profileChanged && over.Users is null;
        var keepTimeShape = !profileChanged && over.Duration is null;

        return new LoadPlan
        {
            Profile = over.Profile ?? Profile,
            Users = over.Users ?? Users,
            Duration = over.Duration ?? Duration,
            Warmup = over.Warmup ?? Warmup,
            RampUp = over.RampUp ?? (keepTimeShape ? RampUp : null),
            Hold = over.Hold ?? (keepTimeShape ? Hold : null),
            RampDown = over.RampDown ?? (keepTimeShape ? RampDown : null),
            BaselineUsers = over.BaselineUsers ?? (keepUserShape ? BaselineUsers : null),
            SpikeDuration = over.SpikeDuration ?? (keepTimeShape ? SpikeDuration : null),
            StartUsers = over.StartUsers ?? (keepUserShape ? StartUsers : null),
            StepUsers = over.StepUsers ?? (keepUserShape ? StepUsers : null),
            StepDuration = over.StepDuration ?? (keepTimeShape ? StepDuration : null),
            Stages = over.Stages ?? (keepUserShape && keepTimeShape ? Stages : null),
        };
    }

    /// <summary>Builds the load profile, deriving any unset shape details from the users and duration.</summary>
    public LoadProfile Build()
    {
        var users = Users ?? Defaults.Users!.Value;
        var duration = Duration ?? Defaults.Duration!.Value;

        switch ((Profile ?? Defaults.Profile)!.ToLowerInvariant())
        {
            case "load":
                return LoadProfiles.Load(users, duration);

            case "soak":
                return LoadProfiles.Soak(users, duration);

            case "spike":
                return LoadProfiles.Spike(BaselineUsers ?? Math.Max(1, users / 4), users, SpikeDuration ?? duration / 5, duration);

            case "stress":
                var step = StepUsers ?? Math.Max(1, users / 5);
                return LoadProfiles.Stress(StartUsers ?? step, users, step, StepDuration ?? duration / 5);

            case "ramp":
                var rampUp = RampUp ?? duration / 3;
                var rampDown = RampDown ?? duration / 3;
                var hold = Hold ?? (duration - rampUp - rampDown is { } rest && rest > TimeSpan.Zero ? rest : TimeSpan.Zero);
                return LoadProfiles.Ramp(users, rampUp, hold, rampDown);

            case "custom":
                if (Stages is not { Count: > 0 })
                    throw new ArgumentException("A custom profile needs Stages. Overriding its users or duration discards them, so set --profile too.");
                return new LoadProfile("Custom", Stages);

            default:
                throw new ArgumentException($"Unknown load profile '{Profile}'. Valid values: load, soak, spike, stress, ramp, custom.");
        }
    }

    private static TimeSpan? Parse(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (Durations.TryParse(value, out var duration))
            return duration;
        throw new FormatException($"{field}: invalid duration '{value}'. Use a value such as 30s, 5m, 1h30m or 00:05:00 (a bare number is read as minutes).");
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
