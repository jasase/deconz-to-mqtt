using DeconzToMqtt.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeconzToMqtt
{
    public class DeconzItemListConverter<TDeconzItem> : JsonConverter
        where TDeconzItem : DeconzItem
    {
        public override bool CanConvert(Type objectType)
            => objectType == typeof(TDeconzItem[]);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var results = new List<TDeconzItem>();

            if (reader.TokenType == JsonToken.StartObject)
            {
                reader.Read();
            }

            while (reader.TokenType != JsonToken.EndObject)
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    var id = reader.Value.ToString();
                    reader.Read();

                    if (reader.TokenType == JsonToken.StartObject)
                    {
                        var sensor = serializer.Deserialize<TDeconzItem>(reader);
                        sensor.Id = Convert.ToInt32(id);
                        results.Add(sensor);
                    }

                    if (reader.TokenType == JsonToken.EndObject)
                    {
                        reader.Read();
                    }
                }
            }

            reader.Read();

            return results.ToArray();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
