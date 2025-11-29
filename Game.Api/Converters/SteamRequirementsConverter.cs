using System.Text.Json;
using System.Text.Json.Serialization;

namespace Game.Api.Converters;

public class SteamRequirementsConverter : JsonConverter<SystemRequirements?>
{
    public override SystemRequirements? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            reader.Skip(); 
            return null;
        }
        
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }
        
        return JsonSerializer.Deserialize<SystemRequirements>(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, SystemRequirements? value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}