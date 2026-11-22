using System.Text.Json;

namespace Pacer.History;

/// <summary>Shared System.Text.Json settings for the file-based stores.</summary>
internal static class StoreJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static readonly JsonSerializerOptions Indented = new(Options)
    {
        WriteIndented = true,
    };
}
