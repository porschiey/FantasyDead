namespace FantasyDead.Data.Configuration
{
    using System;
    using Newtonsoft.Json;

    public class DateTimeConvertor : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(DateTime) == objectType;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {

            if (reader.TokenType == JsonToken.None || reader.TokenType == JsonToken.Null)
                return null;

            if (reader.TokenType != JsonToken.String)
            {
                throw new Exception(
                    String.Format("Unexpected token parsing date. Expected String, got {0}.",
                    reader.TokenType));
            }

            DateTime parsed;
            if (DateTime.TryParse((string)reader.Value, out parsed)) //try normal parsing first
            {
                return parsed;
            }

            var ticks = (string)reader.Value;

            if (Convert.ToInt64(ticks) < 0)
                return DateTime.MinValue;

            return new DateTime(Convert.ToInt64(ticks));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var ticks = string.Empty;
            if (value is DateTime)
            {
                DateTime dt = (DateTime)value;
                if (!dt.Equals(DateTime.MinValue))
                    ticks = dt.Ticks.ToString();
                else
                    ticks = int.MinValue.ToString();
            }
            else
            {
                throw new Exception("Expected date object value.");
            }

            writer.WriteValue(ticks);
        }
    }
}
