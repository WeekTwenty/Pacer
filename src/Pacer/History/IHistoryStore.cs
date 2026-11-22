namespace Pacer.History;

/// <summary>
/// Appends and reads cross-run performance history. The default <see cref="JsonHistoryStore"/> writes
/// one JSON-lines file per scenario under the data directory; replace it via dependency injection to
/// use another backend (e.g. SQLite).
/// </summary>
public interface IHistoryStore
{
    /// <summary>Appends one run to the history for its scenario.</summary>
    Task AppendAsync(string dataDirectory, RunRecord record, CancellationToken cancellationToken = default);

    /// <summary>Reads every recorded run for the scenario, oldest first.</summary>
    Task<IReadOnlyList<RunRecord>> LoadAsync(string dataDirectory, string scenario, CancellationToken cancellationToken = default);

    /// <summary>Lists the scenarios that have at least one recorded run.</summary>
    Task<IReadOnlyList<string>> ListScenariosAsync(string dataDirectory, CancellationToken cancellationToken = default);
}
