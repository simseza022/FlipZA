using System.Globalization;
using System.Text.Json;

namespace FlipZA.Infrastructure.Json;

/// <summary>
/// Null-tolerant navigation over scraped JSON: a missing or wrongly-typed field yields null
/// instead of throwing, so small upstream schema changes degrade data rather than break the run.
/// </summary>
internal static class JsonElementExtensions
{
    public static JsonElement? Prop(this JsonElement element, params string[] path)
    {
        var current = element;
        foreach (var name in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(name, out current))
                return null;
        }

        return current.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined ? null : current;
    }

    public static JsonElement? Prop(this JsonElement? element, params string[] path) =>
        element?.Prop(path);

    public static string? AsString(this JsonElement? element) => element?.ValueKind switch
    {
        JsonValueKind.String => element.Value.GetString(),
        JsonValueKind.Number => element.Value.GetRawText(),
        _ => null
    };

    public static decimal? AsDecimal(this JsonElement? element) => element?.ValueKind switch
    {
        JsonValueKind.Number => element.Value.GetDecimal(),
        JsonValueKind.String when decimal.TryParse(element.Value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var d) => d,
        _ => null
    };

    public static int? AsInt(this JsonElement? element) => element?.ValueKind switch
    {
        JsonValueKind.Number when element.Value.TryGetInt32(out var i) => i,
        JsonValueKind.String when int.TryParse(element.Value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) => i,
        _ => null
    };

    public static bool? AsBool(this JsonElement? element) => element?.ValueKind switch
    {
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => null
    };

    public static IEnumerable<JsonElement> Items(this JsonElement? element) =>
        element?.ValueKind == JsonValueKind.Array ? element.Value.EnumerateArray() : [];
}
