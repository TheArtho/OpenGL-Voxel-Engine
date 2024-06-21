using ConsoleApp1.Source.Mesh;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Utils.Converters;

public class BedrockJsonConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is UvSimple parameterAsList)
        {
            serializer.Serialize(writer, parameterAsList.Uv);
        }
        else if (value is UvSplit parameterAsObject)
        {
            serializer.Serialize(writer, parameterAsObject);
        }
        else
        {
            throw new JsonSerializationException("Invalid parameter type");
        }
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        JToken token = JToken.Load(reader);
        if (token.Type == JTokenType.Array)
        {
            return new UvSimple() { Uv = token.ToObject<List<float>>() };
        }
        else if (token.Type == JTokenType.Object)
        {
            return token.ToObject<UvSplit>();
        }
        throw new JsonSerializationException("Invalid parameter type");
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(Uv).IsAssignableFrom(objectType);
    }
}