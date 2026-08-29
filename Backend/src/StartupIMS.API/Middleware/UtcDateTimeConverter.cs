using System.Text.Json;
using System.Text.Json.Serialization;

namespace StartupIMS.API.Middleware;

/// <summary>
/// MySQL's DATETIME column has no timezone info, so EF Core reads values back
/// with DateTimeKind.Unspecified - even though we always write DateTime.UtcNow.
/// Without this converter, System.Text.Json serializes those as e.g.
/// "2026-08-07T10:23:00" (no "Z"), and JavaScript's `new Date(...)` then
/// interprets that string as LOCAL time, silently shifting every timestamp
/// by the browser's timezone offset. This converter forces every DateTime to
/// be treated and serialized as UTC, so the frontend always gets a correct,
/// unambiguous instant it can convert to the user's local time itself.
/// </summary>
public class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetDateTime();
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utcValue = value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);

        writer.WriteStringValue(utcValue); // serializes with trailing "Z"
    }
}
