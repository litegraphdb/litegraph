namespace LiteGraph.Sdk
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Text.Json.Serialization.Metadata;
    using System.Threading;
    using ExpressionTree;

    /// <summary>
    /// JSON serializer.
    /// Type metadata comes from <see cref="LiteGraphSdkJsonContext"/> first, then from any resolvers added through
    /// <see cref="AddTypeInfoResolver(IJsonTypeInfoResolver)"/>, and finally, only when reflection-based serialization is
    /// enabled (always under the JIT, never under Native AOT by default), from reflection.
    /// Under Native AOT, serializing or deserializing a type that no resolver knows throws <see cref="NotSupportedException"/>.
    /// Thread safety: safe to use from multiple threads.
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

        private const int _DeserializeOptionsIndex = 0;
        private const int _SerializeCompactOptionsIndex = 1;
        private const int _SerializePrettyOptionsIndex = 2;
        private const int _SerializeCompactWithNullsOptionsIndex = 3;
        private const int _SerializePrettyWithNullsOptionsIndex = 4;

        private const string _ReflectionJustification =
            "Reflection-based serialization is only used when JsonSerializer.IsReflectionEnabledByDefault is true, "
            + "which the trimmer sets to false for trimmed and Native AOT applications.";

        private static readonly object _ResolverLock = new object();
        private static List<IJsonTypeInfoResolver> _AdditionalResolvers = new List<IJsonTypeInfoResolver>();
        private static JsonSerializerOptions[] _OptionSets = CreateOptionSets(_AdditionalResolvers);

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        /// <summary>
        /// Add a resolver that supplies JSON metadata for application types, such as a source-generated
        /// <see cref="JsonSerializerContext"/>. Needed under Native AOT for types the SDK does not know about, for example
        /// application classes stored in <see cref="Node.Data"/>.
        /// Resolvers are consulted after <see cref="LiteGraphSdkJsonContext"/> and in the order they were added.
        /// Adding the same resolver instance again has no effect.
        /// Thread safety: safe to call from multiple threads, also while serialization is in progress.
        /// </summary>
        /// <param name="resolver">Resolver, for example <c>MyJsonContext.Default</c>.</param>
        /// <exception cref="ArgumentNullException">Thrown when resolver is null.</exception>
        public static void AddTypeInfoResolver(IJsonTypeInfoResolver resolver)
        {
            ArgumentNullException.ThrowIfNull(resolver);

            lock (_ResolverLock)
            {
                if (_AdditionalResolvers.Contains(resolver)) return;

                List<IJsonTypeInfoResolver> resolvers = new List<IJsonTypeInfoResolver>(_AdditionalResolvers);
                resolvers.Add(resolver);
                JsonSerializerOptions[] optionSets = CreateOptionSets(resolvers);

                _AdditionalResolvers = resolvers;
                Volatile.Write(ref _OptionSets, optionSets);
            }
        }

        /// <summary>
        /// Deserialize JSON to an instance.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="json">JSON bytes.</param>
        /// <returns>Instance.</returns>
        /// <exception cref="NotSupportedException">Thrown when no JSON metadata is available for <typeparamref name="T"/>.</exception>
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
        /// <exception cref="NotSupportedException">Thrown when no JSON metadata is available for <typeparamref name="T"/>.</exception>
        public static T DeserializeJson<T>(string json)
        {
            return JsonSerializer.Deserialize(json, (JsonTypeInfo<T>)GetTypeInfo(typeof(T), _DeserializeOptionsIndex));
        }

        /// <summary>
        /// Deserialize JSON using explicit type metadata, for example <c>MyJsonContext.Default.MyType</c>.
        /// Works under Native AOT without registering a resolver.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="json">JSON string.</param>
        /// <param name="typeInfo">Type metadata.</param>
        /// <returns>Instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when typeInfo is null.</exception>
        public static T DeserializeJson<T>(string json, JsonTypeInfo<T> typeInfo)
        {
            ArgumentNullException.ThrowIfNull(typeInfo);
            return JsonSerializer.Deserialize(json, typeInfo);
        }

        /// <summary>
        /// Serialize object to JSON.
        /// </summary>
        /// <param name="obj">Object.</param>
        /// <param name="pretty">Pretty print.</param>
        /// <returns>JSON.</returns>
        /// <exception cref="NotSupportedException">Thrown when no JSON metadata is available for the run-time type of obj.</exception>
        public static string SerializeJson(object obj, bool pretty = true)
        {
            if (obj == null) return null;

            int index;
            if (IncludeNullProperties) index = pretty ? _SerializePrettyWithNullsOptionsIndex : _SerializeCompactWithNullsOptionsIndex;
            else index = pretty ? _SerializePrettyOptionsIndex : _SerializeCompactOptionsIndex;

            // Every exception is written by the exception converter, so all exception types share its metadata.
            Type type = obj is Exception ? typeof(Exception) : obj.GetType();
            return JsonSerializer.Serialize(obj, GetTypeInfo(type, index));
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

        private static JsonTypeInfo GetTypeInfo(Type type, int index)
        {
            JsonSerializerOptions options = Volatile.Read(ref _OptionSets)[index];
            try
            {
                return options.GetTypeInfo(type);
            }
            catch (NotSupportedException e) when (!JsonSerializer.IsReflectionEnabledByDefault)
            {
                throw new NotSupportedException(
                    "No JSON metadata is available for type '" + type.FullName + "'. Reflection-based serialization is disabled "
                    + "(Native AOT or trimming); add a JsonSerializerContext that includes this type with "
                    + "Serializer.AddTypeInfoResolver.",
                    e);
            }
        }

        private static JsonSerializerOptions[] CreateOptionSets(List<IJsonTypeInfoResolver> additionalResolvers)
        {
            JsonSerializerOptions[] ret = new JsonSerializerOptions[5];
            ret[_DeserializeOptionsIndex] = CreateBaseOptions();
            ret[_SerializeCompactOptionsIndex] = CreateBaseOptions(false, false);
            ret[_SerializePrettyOptionsIndex] = CreateBaseOptions(true, false);
            ret[_SerializeCompactWithNullsOptionsIndex] = CreateBaseOptions(false, true);
            ret[_SerializePrettyWithNullsOptionsIndex] = CreateBaseOptions(true, true);

            foreach (JsonSerializerOptions options in ret)
            {
                options.Converters.Add(new ExceptionConverter<Exception>());
                options.Converters.Add(new NameValueCollectionConverter());

                // Keeps the pre-AOT behavior of writing enums of unregistered types (application classes in Data) as strings.
                if (JsonSerializer.IsReflectionEnabledByDefault) options.Converters.Add(CreateReflectionEnumConverter());

                options.Converters.Add(new DateTimeConverter());
                options.Converters.Add(new IPAddressConverter());
                options.Converters.Add(new ExpressionConverter());

                List<IJsonTypeInfoResolver> chain = new List<IJsonTypeInfoResolver>
                {
                    LiteGraphSdkJsonContext.Default,
                    new ConverterTypeInfoResolver()
                };

                chain.AddRange(additionalResolvers);

                // Keeps the pre-AOT behavior for types the SDK does not know about whenever reflection is available.
                if (JsonSerializer.IsReflectionEnabledByDefault) chain.Add(CreateReflectionResolver());

                options.TypeInfoResolver = JsonTypeInfoResolver.Combine(chain.ToArray());
                options.MakeReadOnly();
            }

            return ret;
        }

        private static JsonSerializerOptions CreateBaseOptions()
        {
            return new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };
        }

        private static JsonSerializerOptions CreateBaseOptions(bool pretty, bool includeNulls)
        {
            JsonSerializerOptions options = CreateBaseOptions();
            if (!includeNulls) options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.WriteIndented = pretty;
            return options;
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", Justification = _ReflectionJustification)]
        [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode", Justification = _ReflectionJustification)]
        private static IJsonTypeInfoResolver CreateReflectionResolver()
        {
            return new DefaultJsonTypeInfoResolver();
        }

        [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode", Justification = _ReflectionJustification)]
        private static JsonConverter CreateReflectionEnumConverter()
        {
            return new JsonStringEnumConverter();
        }

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
                if (JsonSerializer.IsReflectionEnabledByDefault) WriteWithReflection(writer, value, options);
                else WriteExplicit(writer, value as Exception, options);
            }

            [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", Justification = _ReflectionJustification)]
            [UnconditionalSuppressMessage("Trimming", "IL2075:DynamicallyAccessedMembers", Justification = _ReflectionJustification)]
            [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode", Justification = _ReflectionJustification)]
            private static void WriteWithReflection(Utf8JsonWriter writer, TExceptionType value, JsonSerializerOptions options)
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

            private static void WriteExplicit(Utf8JsonWriter writer, Exception value, JsonSerializerOptions options)
            {
                // Same property names and order the reflection path produces for System.Exception, plus ParamName for
                // argument exceptions. Other subclass-specific properties are not written.
                bool skipNulls = options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingNull;

                writer.WriteStartObject();
                WriteString(writer, "Message", value.Message, skipNulls);
                if (value is ArgumentException argumentException) WriteString(writer, "ParamName", argumentException.ParamName, skipNulls);

                writer.WritePropertyName("Data");
                writer.WriteStartObject();
                foreach (System.Collections.DictionaryEntry entry in value.Data)
                {
                    string key = Convert.ToString(entry.Key, CultureInfo.InvariantCulture) ?? String.Empty;
                    if (entry.Value == null) writer.WriteNull(key);
                    else writer.WriteString(key, Convert.ToString(entry.Value, CultureInfo.InvariantCulture));
                }
                writer.WriteEndObject();

                if (value.InnerException != null)
                {
                    writer.WritePropertyName("InnerException");
                    WriteExplicit(writer, value.InnerException, options);
                }
                else if (!skipNulls)
                {
                    writer.WriteNull("InnerException");
                }

                WriteString(writer, "HelpLink", value.HelpLink, skipNulls);
                WriteString(writer, "Source", value.Source, skipNulls);
                writer.WriteNumber("HResult", value.HResult);
                WriteString(writer, "StackTrace", value.StackTrace, skipNulls);
                writer.WriteEndObject();
            }

            private static void WriteString(Utf8JsonWriter writer, string name, string value, bool skipNulls)
            {
                if (value != null) writer.WriteString(name, value);
                else if (!skipNulls) writer.WriteNull(name);
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

                    writer.WriteStartObject();
                    foreach (KeyValuePair<string, string> entry in val)
                    {
                        if (entry.Value == null) writer.WriteNull(entry.Key);
                        else writer.WriteString(entry.Key, entry.Value);
                    }
                    writer.WriteEndObject();
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
                // Timestamps are UTC. A "Z" or "+hh:mm" suffix is honored and the result converted to UTC; a value without
                // one is taken as UTC. Without these styles, TryParse converts zoned values to the machine's local time,
                // which the writer then labels "Z", shifting the timestamp by the local UTC offset on every round trip.
                if (DateTime.TryParse(str, CultureInfo.CurrentCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out val)) return val;

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
                // Timestamps are UTC, so local times are converted before formatting.
                if (value.Kind == DateTimeKind.Local) value = value.ToUniversalTime();
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
                        return Read(ref reader, typeof(Expr), null);
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