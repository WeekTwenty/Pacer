namespace Pacer.History;

/// <summary>Paths and file-name helpers shared by the file-based history and baseline stores.</summary>
internal static class StorePaths
{
    public static string HistoryDirectory(string dataDirectory) => Path.Combine(dataDirectory, "history");

    public static string BaselineDirectory(string dataDirectory) => Path.Combine(dataDirectory, "baselines");

    /// <summary>Makes a scenario name safe to use as a file name.</summary>
    public static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.Select(c => invalid.Contains(c) || char.IsWhiteSpace(c) ? '_' : c).ToArray();
        return new string(chars);
    }
}
