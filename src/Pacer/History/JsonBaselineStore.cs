using System.Text.Json;

namespace Pacer.History;

/// <summary>
/// The default <see cref="IBaselineStore"/>: writes one readable JSON file per named baseline under
/// <c>&lt;dataDirectory&gt;/baselines/&lt;scenario&gt;/&lt;baseline&gt;.json</c>.
/// </summary>
public sealed class JsonBaselineStore : IBaselineStore
{
    private sealed record BaselineFile(string Name, RunRecord Record);

    /// <inheritdoc />
    public async Task SaveAsync(string dataDirectory, string baselineName, RunRecord baseline, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(baselineName);
        ArgumentNullException.ThrowIfNull(baseline);

        var directory = BaselineDirectory(dataDirectory, baseline.ScenarioName);
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, $"{StorePaths.Sanitize(baselineName)}.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new BaselineFile(baselineName, baseline), StoreJson.Indented), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<RunRecord?> LoadAsync(string dataDirectory, string scenario, string baselineName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(scenario);
        ArgumentException.ThrowIfNullOrWhiteSpace(baselineName);

        var path = BaselinePath(dataDirectory, scenario, baselineName);
        if (!File.Exists(path))
            return null;

        var file = JsonSerializer.Deserialize<BaselineFile>(await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false), StoreJson.Options);
        return file?.Record;
    }

    /// <inheritdoc />
    public Task DeleteAsync(string dataDirectory, string scenario, string baselineName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(scenario);
        ArgumentException.ThrowIfNullOrWhiteSpace(baselineName);

        var path = BaselinePath(dataDirectory, scenario, baselineName);
        if (File.Exists(path))
            File.Delete(path);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ListScenariosAsync(string dataDirectory, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);

        var directory = StorePaths.BaselineDirectory(dataDirectory);
        if (!Directory.Exists(directory))
            return [];

        var names = new List<string>();
        foreach (var scenarioDirectory in Directory.EnumerateDirectories(directory))
        {
            var first = Directory.EnumerateFiles(scenarioDirectory, "*.json").FirstOrDefault();
            if (first is null)
                continue;

            var file = JsonSerializer.Deserialize<BaselineFile>(await File.ReadAllTextAsync(first, cancellationToken).ConfigureAwait(false), StoreJson.Options);
            if (file?.Record is not null)
                names.Add(file.Record.ScenarioName);
        }

        return names.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ListBaselinesAsync(string dataDirectory, string scenario, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(scenario);

        var directory = BaselineDirectory(dataDirectory, scenario);
        if (!Directory.Exists(directory))
            return [];

        var names = new List<string>();
        foreach (var path in Directory.EnumerateFiles(directory, "*.json"))
        {
            var file = JsonSerializer.Deserialize<BaselineFile>(await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false), StoreJson.Options);
            if (file is not null)
                names.Add(file.Name);
        }

        return names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static string BaselineDirectory(string dataDirectory, string scenario)
        => Path.Combine(StorePaths.BaselineDirectory(dataDirectory), StorePaths.Sanitize(scenario));

    private static string BaselinePath(string dataDirectory, string scenario, string baselineName)
        => Path.Combine(BaselineDirectory(dataDirectory, scenario), $"{StorePaths.Sanitize(baselineName)}.json");
}
