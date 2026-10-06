namespace LiteGraph.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Text.Json.Serialization.Metadata;
    using System.Threading;
    using ExpressionTree;

    /// <summary>
    /// JSON serialization for LiteGraph objects.
    /// Type metadata comes from <see cref="LiteGraphJsonContext"/> first, then from any resolvers added through
    /// <see cref="AddTypeInfoResolver(IJsonTypeInfoResolver)"/>, and finally, only when reflection-based serialization is
    /// enabled (always under the JIT, never under Native AOT by default), from reflection.
    /// Under Native AOT, serializing or deserializing a type that no resolver knows throws <see cref="NotSupportedException"/>.
    /// Thread safety: instances and the static members are safe to use from multiple threads.
    /// </summary>
    public class Serializer : ISerializer
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private const int _DeserializeOptionsIndex = 0;
        private const int _SerializeOptionsIndex = 1;
        private const int _PrettySerializeOptionsIndex = 2;

        private const string _ReflectionJustification =
            "Reflection-based serialization is only used when JsonSerializer.IsReflectionEnabledByDefault is true, "
            + "which the trimmer sets to false for trimmed and Native AOT applications.";

        private static readonly object _ResolverLock = new object();
        private static List<IJsonTypeInfoResolver> _AdditionalResolvers = new List<IJsonTypeInfoResolver>();
        private static JsonSerializerOptions[] _Options = CreateOptionSets(_AdditionalResolvers);

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the serializer.
        /// </summary>
        public Serializer()
        {
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Add a resolver that supplies JSON metadata for application types, such as a source-generated
        /// <see cref="JsonSerializerContext"/>. Needed under Native AOT for types LiteGraph does not know about, for example
        /// application classes stored in <see cref="Node.Data"/> or read with <see cref="LiteGraphClient.ConvertData{T}(object)"/>.
        /// Resolvers are consulted after <see cref="LiteGraphJsonContext"/> and in the order they were added.
        /// Adding the same resolver instance again has no effect.
        /// Takes effect for every <see cref="Serializer"/> instance, including those already created.
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
                JsonSerializerOptions[] options = CreateOptionSets(resolvers);

                _AdditionalResolvers = resolvers;
                Volatile.Write(ref _Options, options);
            }
        }

        /// <inheritdoc/>
        /// <exception cref="NotSupportedException">Thrown when no JSON metadata is available for <typeparamref name="T"/>.</exception>
        public T CopyObject<T>(T obj)
        {
            if (obj == null) return default(T);

            // Resolve metadata before the try block so a missing registration fails loudly instead of returning default.
            JsonTypeInfo<T> typeInfo = GetTypeInfo<T>(_DeserializeOptionsIndex);
            string json = SerializeJson(obj, false);
            try
            {
                return JsonSerializer.Deserialize(json, typeInfo);
            }
            catch (Exception)
            {
                return default(T);
            }
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException">Thrown when json is null or empty.</exception>
        /// <exception cref="NotSupportedException">Thrown when no JSON metadata is available for <typeparamref name="T"/>.</exception>
        /// <exception cref="JsonException">Thrown when the JSON is invalid or does not match <typeparamref name="T"/>.</exception>
        public T DeserializeJson<T>(string json)
        {
            if (String.IsNullOrEmpty(json)) throw new ArgumentNullException(nameof(json));
            return JsonSerializer.Deserialize(json, GetTypeInfo<T>(_DeserializeOptionsIndex));
        }

        /// <summary>
        /// Deserialize JSON using explicit type metadata, for example <c>MyJsonContext.Default.MyType</c>.
        /// Works under Native AOT without registering a resolver.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="json">JSON.</param>
        /// <param name="typeInfo">Type metadata.</param>
        /// <returns>Instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when json is null or empty, or typeInfo is null.</exception>
        /// <exception cref="JsonException">Thrown when the JSON is invalid or does not match <typeparamref name="T"/>.</exception>
        public T DeserializeJson<T>(string json, JsonTypeInfo<T> typeInfo)
        {
            if (String.IsNullOrEmpty(json)) throw new ArgumentNullException(nameof(json));
            ArgumentNullException.ThrowIfNull(typeInfo);
            return JsonSerializer.Deserialize(json, typeInfo);
        }

        /// <inheritdoc/>
        /// <exception cref="NotSupportedException">Thrown when no JSON metadata is available for the run-time type of obj.</exception>
        public string SerializeJson(object obj, bool pretty = false)
        {
            if (obj == null) return null;
            int index = pretty ? _PrettySerializeOptionsIndex : _SerializeOptionsIndex;

            // Every exception is written by the exception converter, so all exception types share its metadata.
            Type type = obj is Exception ? typeof(Exception) : obj.GetType();
            return JsonSerializer.Serialize(obj, GetTypeInfo(type, index));
        }

        #endregion

        #region Private-Methods

        private static JsonTypeInfo<T> GetTypeInfo<T>(int index)
        {
            return (JsonTypeInfo<T>)GetTypeInfo(typeof(T), index);
        }

        private static JsonTypeInfo GetTypeInfo(Type type, int index)
        {
            JsonSerializerOptions options = Volatile.Read(ref _Options)[index];
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
            JsonSerializerOptions[] ret = new JsonSerializerOptions[3];

            ret[_DeserializeOptionsIndex] = new JsonSerializerOptions();

            ret[_SerializeOptionsIndex] = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false
            };

            ret[_PrettySerializeOptionsIndex] = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            };

            foreach (JsonSerializerOptions options in ret)
            {
                AddCommonConverters(options);

                // Keeps the pre-AOT behavior of writing enums of unregistered types (application classes in Data) as strings.
                if (JsonSerializer.IsReflectionEnabledByDefault) options.Converters.Add(CreateReflectionEnumConverter());

                options.TypeInfoResolver = CreateResolver(additionalResolvers);
                options.MakeReadOnly();
            }

            return ret;
        }

        /// <summary>
        /// Build the resolver chain LiteGraph uses: <see cref="LiteGraphJsonContext"/>, converter-backed root types, the
        /// given resolvers, and reflection when it is enabled.
        /// </summary>
        /// <param name="additionalResolvers">Resolvers to consult after LiteGraph's own metadata; may be null.</param>
        /// <returns>Combined resolver.</returns>
        internal static IJsonTypeInfoResolver CreateResolver(IEnumerable<IJsonTypeInfoResolver> additionalResolvers)
        {
            List<IJsonTypeInfoResolver> chain = new List<IJsonTypeInfoResolver>
            {
                LiteGraphJsonContext.Default,
                new ConverterTypeInfoResolver()
            };

            if (additionalResolvers != null) chain.AddRange(additionalResolvers);

            // Keeps the pre-AOT behavior for types LiteGraph does not know about (application classes in Data, anonymous
            // types, ConvertData<T>) whenever reflection is available.
            if (JsonSerializer.IsReflectionEnabledByDefault) chain.Add(CreateReflectionResolver());

            return JsonTypeInfoResolver.Combine(chain.ToArray());
        }

        /// <summary>
        /// Get typed metadata from options whose resolver was built with <see cref="CreateResolver"/>.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="options">Options.</param>
        /// <returns>Type metadata.</returns>
        internal static JsonTypeInfo<T> GetTypeInfo<T>(JsonSerializerOptions options)
        {
            return (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T));
        }

        private static void AddCommonConverters(JsonSerializerOptions options)
        {
            options.Converters.Add(new ExceptionConverter<Exception>());
            options.Converters.Add(new NameValueCollectionConverter());
            options.Converters.Add(new DateTimeConverter());
            options.Converters.Add(new IPAddressConverter());
            options.Converters.Add(new ExpressionConverter());
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

        private class ExceptionConverter<TExceptionType> : JsonConverter<TExceptionType>
        {
            public override bool CanConvert(Type typeToConvert)
            {
                return typeof(Exception).IsAssignableFrom(typeToConvert);
            }

            public override TExceptionType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                throw new NotSupportedException("Deserializing exceptions is not allowed");
            }

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
                foreach (DictionaryEntry entry in value.Data)
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

        private class NameValueCollectionConverter : JsonConverter<NameValueCollection>
        {
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

            public override void Write(Utf8JsonWriter writer, NameValueCollection value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                foreach (string key in value.Keys.Cast<string>())
                {
                    string[] values = value.GetValues(key);
                    if (values == null) writer.WriteNull(key ?? String.Empty);
                    else writer.WriteString(key ?? String.Empty, string.Join(", ", values));
                }
                writer.WriteEndObject();
            }
        }

        private class IPAddressConverter : JsonConverter<IPAddress>
        {
            public override IPAddress Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                string str = reader.GetString();
                return IPAddress.Parse(str);
            }

            public override void Write(Utf8JsonWriter writer, IPAddress value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value.ToString());
            }
        }

        private class DateTimeConverter : JsonConverter<DateTime>
        {
            public override DateTime Read(
                        ref Utf8JsonReader reader,
                        Type typeToConvert,
                        JsonSerializerOptions options)
            {
                string str = reader.GetString();

                DateTime val;
                // Timestamps are UTC. A "Z" or "+hh:mm" suffix is honored and the result converted to UTC; a value without
                // one is taken as UTC. Without these styles, TryParse converts zoned values to the machine's local time,
                // which the writer then labels "Z", shifting the timestamp by the local UTC offset on every round trip.
                if (DateTime.TryParse(str, CultureInfo.CurrentCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out val)) return val;

                throw new FormatException("The JSON value '" + str + "' could not be converted to System.DateTime.");
            }

            public override void Write(
                Utf8JsonWriter writer,
                DateTime dateTimeValue,
                JsonSerializerOptions options)
            {
                // The output carries a "Z" suffix, so local times are converted to UTC first.
                if (dateTimeValue.Kind == DateTimeKind.Local) dateTimeValue = dateTimeValue.ToUniversalTime();

                writer.WriteStringValue(dateTimeValue.ToString(
                    "yyyy-MM-ddTHH:mm:ss.ffffffZ", CultureInfo.InvariantCulture));
            }

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

        private class ExpressionConverter : JsonConverter<Expr>
        {
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
                else if (value is bool b)
                {
                    writer.WriteBooleanValue(b);
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
    }
}
