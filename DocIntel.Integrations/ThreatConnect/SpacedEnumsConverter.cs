using System;

using Newtonsoft.Json;

namespace DocIntel.Integrations.ThreatConnect
{
    public class SpacedEnumsConverter<T> : JsonConverter where T: Enum
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
        
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var enumString = (string)reader.Value;
            return Enum.Parse(typeof(T), enumString.Replace(" ", ""), true);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }
    }
}