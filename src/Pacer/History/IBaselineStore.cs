namespace Pacer.History;

/// <summary>
/// Persists named baselines per scenario. The default <see cref="JsonBaselineStore"/> writes one JSON
/// file per baseline under the data directory; replace it via dependency injection to use another
/// backend.
/// </summary>
public interface IBaselineStore
{
    /// <summary>Saves (or overwrites) a named baseline for a scenario.</summary>
    Task SaveAsync(string dataDirectory, string baselineName, RunRecord baseline, CancellationToken cancellationToken = default);

    /// <summary>Loads a named baseline, or <see langword="null"/> if none is saved.</summary>
    Task<RunRecord?> LoadAsync(string dataDirectory, string scenario, string baselineName, CancellationToken cancellationToken = default);

    /// <summary>Deletes a named baseline. No-op when none is saved.</summary>
    Task DeleteAsync(string dataDirectory, string scenario, string baselineName, CancellationToken cancellationToken = default);

    /// <summary>Lists the scenarios that have at least one saved baseline.</summary>
    Task<IReadOnlyList<string>> ListScenariosAsync(string dataDirectory, CancellationToken cancellationToken = default);

    /// <summary>Lists the baseline names saved for a scenario.</summary>
    Task<IReadOnlyList<string>> ListBaselinesAsync(string dataDirectory, string scenario, CancellationToken cancellationToken = default);
}
