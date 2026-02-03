namespace LiteGraph.Sdk
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using ExpressionTree;

    /// <summary>
    /// JSON serializer.
    /// </summary>
    public static class Serializer
    {
        #region Public-Members

        /// <summary>
        /// DateTime format.
        /// </summary>
        public static string DateTimeFormat
        {
            get
            {
                return _DateTimeFormat;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(DateTimeFormat));
                _DateTimeFormat = value;
            }
        }

        /// <summary>
        /// True to include null properties when serializing, false to not include null properties when serializing.
        /// </summary>
        public static bool IncludeNullProperties { get; set; } = false;

        #endregion

        #region Private-Members

        private static string _DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.ffffffZ";

        private static JsonSerializerOptions _Options = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        /// <summary>
        /// Deserialize JSON to an instance.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="json">JSON bytes.</param>
        /// <returns>Instance.</returns>
        public static T DeserializeJson<T>(byte[] json)
        {
            return DeserializeJson<T>(Encoding.UTF8.GetString(json));
        }

        /// <summary>
        /// Deserialize JSON to an instance.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="json">JSON string.</param>
        /// <returns>Instance.</returns>
        public static T DeserializeJson<T>(string json)
        {
            JsonSerializerOptions options = new JsonSerializerOptions(_Options);

            options.Converters.Add(new ExceptionConverter<Exception>());
            options.Converters.Add(new NameValueCollectionConverter());
            options.Converters.Add(new JsonStringEnumConverter());
            options.Converters.Add(new DateTimeConverter());
            options.Converters.Add(new IPAddressConverter());
            options.Converters.Add(new ExpressionConverter());

            return JsonSerializer.Deserialize<T>(json, options);
        }

        /// <summary>
        /// Serialize object to JSON.
        /// </summary>
        /// <param name="obj">Object.</param>
        /// <param name="pretty">Pretty print.</param>
        /// <returns>JSON.</returns>
        public static string SerializeJson(object obj, bool pretty = true)
        {
            if (obj == null) return null;

            JsonSerializerOptions options = new JsonSerializerOptions(_Options);
            if (!IncludeNullProperties) options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

            if (!pretty)
            {
                options.WriteIndented = false;

                options.Converters.Add(new ExceptionConverter<Exception>());
                options.Converters.Add(new NameValueCollectionConverter());
                options.Converters.Add(new JsonStringEnumConverter());
                options.Converters.Add(new DateTimeConverter());
                options.Converters.Add(new IPAddressConverter());
                options.Converters.Add(new ExpressionConverter());

                string json = JsonSerializer.Serialize(obj, options);
                options = null;
                return json;
            }
            else
            {
                options.WriteIndented = true;

                options.Converters.Add(new ExceptionConverter<Exception>());
                options.Converters.Add(new NameValueCollectionConverter());
                options.Converters.Add(new JsonStringEnumConverter());
                options.Converters.Add(new DateTimeConverter());
                options.Converters.Add(new IPAddressConverter());
                options.Converters.Add(new ExpressionConverter());

                string json = JsonSerializer.Serialize(obj, options);
                options = null;
                return json;
            }
        }

        /// <summary>
        /// Attempt to JSON serialize an object.  Null inputs will return true.
        /// </summary>
        /// <param name="obj">Object.</param>
        /// <param name="pretty">Pretty.</param>
        /// <param name="json">JSON string.</param>
        /// <returns>True if serialized.</returns>
        public static bool TrySerializeJson(object obj, bool pretty, out string json)
        {
            json = null;

            if (obj == null) return true;

            try
            {
                json = SerializeJson(obj, pretty);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Copy an object.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="o">Object.</param>
        /// <returns>Instance.</returns>
        public static T CopyObject<T>(object o)
        {
            if (o == null) return default(T);
            string json = SerializeJson(o, false);
            T ret = DeserializeJson<T>(json);
            return ret;
        }

        #endregion

        #region Private-Methods

        #endregion

        #region Public-Classes

        /// <summary>
        /// Exception converter.
        /// </summary>
        /// <typeparam name="TExceptionType">Exception type.</typeparam>
        public class ExceptionConverter<TExceptionType> : JsonConverter<TExceptionType>
        {
            /// <summary>
            /// Can convert.
            /// </summary>
            /// <param name="typeToConvert">Type to convert.</param>
            /// <returns>Boolean.</returns>
            public override bool CanConvert(Type typeToConvert)
            {
                return typeof(Exception).IsAssignableFrom(typeToConvert);
            }

            /// <summary>
            /// Read.
            /// </summary>
            /// <param name="reader">Reader.</param>
            /// <param name="typeToConvert">Type to convert.</param>
            /// <param name="options">Options.</param>
            /// <returns>TExceptionType.</returns>
            public override TExceptionType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                throw new NotSupportedException("Deserializing exceptions is not allowed");
            }

            /// <summary>
            /// Write.
            /// </summary>
            /// <param name="writer">Writer.</param>
            /// <param name="value">Value.</param>
            /// <param name="options">Options.</param>
            public override void Write(Utf8JsonWriter writer, TExceptionType value, JsonSerializerOptions options)
            {
                IEnumerable<PropertyInfo> properties = value.GetType()
                    .GetProperties()
                    .Where(p => p.Name != nameof(Exception.TargetSite));

                List<PropertyInfo> propList = properties.ToList();

                if (propList.Count == 0)
                {
                    // Nothing to write
                    return;
                }

                writer.WriteStartObject();

                foreach (PropertyInfo prop in propList)
                {
                    object propValue = prop.GetValue(value);
                    if (options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingNull && propValue == null)
                    {
                        continue;
                    }
                    writer.WritePropertyName(prop.Name);
                    JsonSerializer.Serialize(writer, propValue, options);
                }

                writer.WriteEndObject();
            }
        }

        /// <summary>
        /// Name value collection converter.
        /// </summary>
        public class NameValueCollectionConverter : JsonConverter<NameValueCollection>
        {
            /// <summary>
            /// Read.
            /// </summary>
            /// <param name="reader">Reader.</param>
            /// <param name="typeToConvert">Type to convert.</param>
            /// <param name="options">Options.</param>
            /// <returns>NameValueCollection.</returns>
            public override NameValueCollection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException("Expected start of object");
                }

                NameValueCollection collection = new NameValueCollection();

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        return collection;
                    }

                    if (reader.TokenType != JsonTokenType.PropertyName)
                    {
                        throw new JsonException("Expected property name");
                    }

                    string key = reader.GetString();

                    reader.Read();
                    if (reader.TokenType == JsonTokenType.Null)
                    {
                        collection.Add(key, null);
                        continue;
                    }

                    if (reader.TokenType != JsonTokenType.String)
                    {
                        throw new JsonException("Expected string value");
                    }

                    string value = reader.GetString();

                    // If the value contains commas, split it and add each value separately
                    if (!string.IsNullOrEmpty(value) && value.Contains(","))
                    {
                        IEnumerable<string> values = value.Split(',')
                                        .Select(v => v.Trim());
                        foreach (string v in values)
                        {
                            collection.Add(key, v);
                        }
                    }
                    else
                    {
                        collection.Add(key, value);
                    }
                }

                throw new JsonException("Expected end of object");
            }

            /// <summary>
            /// Write.
            /// </summary>
            /// <param name="writer">Writer.</param>
            /// <param name="value">Value.</param>
            /// <param name="options">Options.</param>
            public override void Write(Utf8JsonWriter writer, NameValueCollection value, JsonSerializerOptions options)
            {
                if (value != null)
                {
                    Dictionary<string, string> val = new Dictionary<string, string>();

                    for (int i = 0; i < value.AllKeys.Count(); i++)
                    {
                        string key = value.Keys[i];
                        string[] values = value.GetValues(key);
                        string formattedValue = null;

                        if (values != null && values.Length > 0)
                        {
                            int added = 0;

                            for (int j = 0; j < values.Length; j++)
                            {
                                if (!String.IsNullOrEmpty(values[j]))
                                {
                                    if (added == 0) formattedValue += values[j];
                                    else formattedValue += ", " + values[j];
                                }

                                added++;
                            }
                        }

                        val.Add(key, formattedValue);
                    }

                    System.Text.Json.JsonSerializer.Serialize(writer, val);
                }
            }
        }

        /// <summary>
        /// DateTime converter.
        /// </summary>
        public class DateTimeConverter : JsonConverter<DateTime>
        {
            /// <summary>
            /// Read.
            /// </summary>
            /// <param name="reader">Reader.</param>
            /// <param name="typeToConvert">Type to convert.</param>
            /// <param name="options">Options.</param>
            /// <returns>NameValueCollection.</returns>
            public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                string str = reader.GetString();

                DateTime val;
                if (DateTime.TryParse(str, out val)) return val;

                throw new FormatException("The JSON value '" + str + "' could not be converted to System.DateTime.");
            }

            /// <summary>
            /// Write.
            /// </summary>
            /// <param name="writer">Writer.</param>
            /// <param name="value">Value.</param>
            /// <param name="options">Options.</param>
            public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value.ToString(_DateTimeFormat, CultureInfo.InvariantCulture));
            }

            /// <summary>
            /// Reserved for future use.
            /// Not used because Read does a TryParse which will evaluate several formats.
            /// </summary>
            private List<string> _AcceptedFormats = new List<string>
            {
                "yyyy-MM-dd HH:mm:ss",
                "yyyy-MM-ddTHH:mm:ss",
                "yyyy-MM-ddTHH:mm:ssK",
                "yyyy-MM-dd HH:mm:ss.ffffff",
                "yyyy-MM-ddTHH:mm:ss.ffffff",
                "yyyy-MM-ddTHH:mm:ss.fffffffK",
                "yyyy-MM-dd",
                "MM/dd/yyyy HH:mm",
                "MM/dd/yyyy hh:mm tt",
                "MM/dd/yyyy H:mm",
                "MM/dd/yyyy h:mm tt",
                "MM/dd/yyyy HH:mm:ss"
            };
        }

        /// <summary>
        /// IntPtr converter.  IntPtr cannot be deserialized.
        /// </summary>
        public class IntPtrConverter : JsonConverter<IntPtr>
        {
            /// <summary>
            /// Read.
            /// </summary>
            /// <param name="reader">Reader.</param>
            /// <param name="typeToConvert">Type to convert.</param>
            /// <param name="options">Options.</param>
            /// <returns>NameValueCollection.</returns>
            public override IntPtr Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                throw new InvalidOperationException("Properties of type IntPtr cannot be deserialized from JSON.");
            }

            /// <summary>
            /// Write.
            /// </summary>
            /// <param name="writer">Writer.</param>
            /// <param name="value">Value.</param>
            /// <param name="options">Options.</param>
            public override void Write(Utf8JsonWriter writer, IntPtr value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value.ToString());
            }
        }

        /// <summary>
        /// IP address converter.
        /// </summary>
        public class IPAddressConverter : JsonConverter<IPAddress>
        {
            /// <summary>
            /// Read.
            /// </summary>
            /// <param name="reader">Reader.</param>
            /// <param name="typeToConvert">Type to convert.</param>
            /// <param name="options">Options.</param>
            /// <returns>NameValueCollection.</returns>
            public override IPAddress Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                string str = reader.GetString();
                return IPAddress.Parse(str);
            }

            /// <summary>
            /// Write.
            /// </summary>
            /// <param name="writer">Writer.</param>
            /// <param name="value">Value.</param>
            /// <param name="options">Options.</param>
            public override void Write(Utf8JsonWriter writer, IPAddress value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value.ToString());
            }
        }

        /// <summary>
        /// Expression converter.
        /// </summary>
        public class ExpressionConverter : JsonConverter<Expr>
        {
            /// <summary>
            /// Read.
            /// </summary>
            /// <param name="reader">Reader.</param>
            /// <param name="typeToConvert">Type to convert.</param>
            /// <param name="options">Options.</param>
            /// <returns>Expr.</returns>
            public override Expr Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException("Expected start of object");
                }

                Expr expr = new Expr();

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        return expr;
                    }

                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        string propertyName = reader.GetString();
                        reader.Read();

                        switch (propertyName)
                        {
                            case "Left":
                                expr.Left = ReadValue(ref reader);
                                break;
                            case "Operator":
                                expr.Operator = Enum.Parse<OperatorEnum>(reader.GetString());
                                break;
                            case "Right":
                                expr.Right = ReadValue(ref reader);
                                break;
                            default:
                                reader.Skip();
                                break;
                        }
                    }
                }

                return expr;
            }

            private object ReadValue(ref Utf8JsonReader reader)
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.String:
                        return reader.GetString();
                    case JsonTokenType.Number:
                        if (reader.TryGetInt64(out long longValue))
                            return longValue;
                        return reader.GetDouble();
                    case JsonTokenType.True:
                        return true;
                    case JsonTokenType.False:
                        return false;
                    case JsonTokenType.Null:
                        return null;
                    case JsonTokenType.StartObject:
                        return JsonSerializer.Deserialize<Expr>(ref reader, new JsonSerializerOptions { Converters = { new ExpressionConverter() } });
                    case JsonTokenType.StartArray:
                        List<object> list = new List<object>();
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                        {
                            list.Add(ReadValue(ref reader));
                        }
                        return list;
                    default:
                        throw new JsonException($"Unexpected token type: {reader.TokenType}");
                }
            }

            /// <summary>
            /// Write.
            /// </summary>
            /// <param name="writer">Writer.</param>
            /// <param name="value">Value.</param>
            /// <param name="options">Options.</param>
            public override void Write(Utf8JsonWriter writer, Expr value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                writer.WritePropertyName("Left");
                WriteValue(writer, value.Left);

                writer.WritePropertyName("Operator");
                writer.WriteStringValue(value.Operator.ToString());

                writer.WritePropertyName("Right");
                WriteValue(writer, value.Right);

                writer.WriteEndObject();
            }

            private void WriteValue(Utf8JsonWriter writer, object value)
            {
                if (value == null)
                {
                    writer.WriteNullValue();
                }
                else if (value is string str)
                {
                    writer.WriteStringValue(str);
                }
                else if (value is long l)
                {
                    writer.WriteNumberValue(l);
                }
                else if (value is int i)
                {
                    writer.WriteNumberValue(i);
                }
                else if (value is double d)
                {
                    writer.WriteNumberValue(d);
                }
                else if (value is float f)
                {
                    writer.WriteNumberValue(f);
                }
                else if (value is bool b)
                {
                    writer.WriteBooleanValue(b);
                }
                else if (value is Guid g)
                {
                    writer.WriteStringValue(g.ToString());
                }
                else if (value is Expr expr)
                {
                    Write(writer, expr, null);
                }
                else if (value is IEnumerable<object> list)
                {
                    writer.WriteStartArray();
                    foreach (object item in list)
                    {
                        WriteValue(writer, item);
                    }
                    writer.WriteEndArray();
                }
                else
                {
                    throw new JsonException($"Unexpected value type: {value.GetType()}");
                }
            }
        }

        #endregion
    }
}