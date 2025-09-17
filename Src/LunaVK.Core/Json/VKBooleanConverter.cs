using Newtonsoft.Json;
using System;

namespace LunaVK.Core.Json
{
    /// <summary>
    /// Представляет Json конвертер 1-0 то <see cref="bool"/>.
    /// </summary>
    public sealed class VKBooleanConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) { return objectType == typeof(bool); }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Be defensive: reader.Value can be null or various types (bool, integer, string)
            var val = reader.Value;
            if (val == null)
                return false;

            if (val is bool bVal)
                return bVal;

            try
            {
                // numeric types
                if (val is int || val is long || val is short || val is byte || val is uint || val is ulong)
                {
                    return Convert.ToInt64(val) != 0;
                }

                // fallback to string parsing
                string temp = val.ToString();
                if (string.IsNullOrEmpty(temp))
                    return false;

                if (long.TryParse(temp, out long n))
                    return n != 0;

                if (bool.TryParse(temp, out bool parsedBool))
                    return parsedBool;

                return temp == "1";
            }
            catch
            {
                return false;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(((bool)value) ? 1 : 0);
        }
    }
}
