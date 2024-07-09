using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Divination.FaloopIntegration.Faloop.Model;

public record DcDataResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("data")] JsonObject Data);

public class ActiveSpawnConverter : JsonConverter<MobReportData>
{
    public override MobReportData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jsonObject = JsonSerializer.Deserialize<JsonElement>(ref reader, options);
        if (jsonObject.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Expected a JSON object.");
        }

        string action = MobReportActions.Spawn;
        var data = JsonObject.Create(JsonSerializer.SerializeToElement(jsonObject))!;

        // zoneInstance is not always present in the json response, so we have to try to get it in a safe way
        var zoneInstance = jsonObject.TryGetProperty("zoneInstance", out var zoneInstanceElement) &&
                           zoneInstanceElement.TryGetInt32(out var instance) ? instance : 0;

        var mobReportIds = new MobReportIds(
            jsonObject.GetProperty("mobId2").GetString()!,
            jsonObject.GetProperty("worldId2").GetString()!,
            zoneInstance);

        return new MobReportData(action, data, mobReportIds);
    }

    public override void Write(Utf8JsonWriter writer, MobReportData value, JsonSerializerOptions options)
    {
        // Implement custom serialization if necessary
        throw new NotImplementedException();
    }
}
