using System.Text.Json;

namespace Pacer.History;

/// <summary>
/// The default <see cref="IHistoryStore"/>: appends one compact JSON object per run to a
/// JSON-lines file per scenario under <c>&lt;dataDirectory&gt;/history/&lt;scenario&gt;.jsonl</c>.
/// </summary>
public sealed class JsonHistoryStore : IHistoryStore
{
    /// <inheritdoc />
    public Task AppendAsync(string dataDirectory, RunRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);
        ArgumentNullException.ThrowIfNull(record);

        var directory = StorePaths.HistoryDirectory(dataDirectory);
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, $"{StorePaths.Sanitize(record.ScenarioName)}.jsonl");
        var line = JsonSerializer.Serialize(record, StoreJson.Options) + Environment.NewLine;
        return File.AppendAllTextAsync(path, line, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RunRecord>> LoadAsync(string dataDirectory, string scenario, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(scenario);

        var path = Path.Combine(StorePaths.HistoryDirectory(dataDirectory), $"{StorePaths.Sanitize(scenario)}.jsonl");
        if (!File.Exists(path))
            return [];

        var records = new List<RunRecord>();
        foreach (var line in await File.ReadAllLinesAsync(path, cancellationToken).ConfigureAwait(false))
        {
            if (!string.IsNullOrWhiteSpace(line))
                records.Add(JsonSerializer.Deserialize<RunRecord>(line, StoreJson.Options)!);
        }

        return records;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ListScenariosAsync(string dataDirectory, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);

        var directory = StorePaths.HistoryDirectory(dataDirectory);
        if (!Directory.Exists(directory))
            return [];

        var names = new List<string>();
        foreach (var path in Directory.EnumerateFiles(directory, "*.jsonl"))
        {
            var first = File.ReadLines(path).FirstOrDefault(line => !string.IsNullOrWhiteSpace(line));
            if (first is null)
                continue;

            var record = JsonSerializer.Deserialize<RunRecord>(first, StoreJson.Options);
            if (record is not null)
                names.Add(record.ScenarioName);
        }

        return names.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
