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
            else
            {
                //try ticks
                try
                {
                    var ticks = Convert.ToInt64(reader.Value);
                    return new DateTime(ticks);
                }
                catch
                {
                    throw new InvalidOperationException("Invalid date, cannot convert"); ;
                }

            }

        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var isoD = string.Empty;
            if (value is DateTime)
            {
                DateTime dt = (DateTime)value;
                if (!dt.Equals(DateTime.MinValue))
                    isoD = dt.ToString("O");
                else
                    isoD = string.Empty;
            }
            else
            {
                throw new Exception("Expected date object value.");
            }

            writer.WriteValue(isoD);
        }
    }
}
