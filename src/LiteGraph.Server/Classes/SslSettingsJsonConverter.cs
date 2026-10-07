namespace LiteGraph.Server.Classes
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using WatsonWebserver.Core.Settings;

    /// <summary>
    /// JSON converter for Watson's <see cref="SslSettings"/> in <see cref="Settings.Rest"/>.
    /// Writes and reads the settings-file fields (Enable, PfxCertificateFile, PfxCertificatePassword, MutuallyAuthenticate,
    /// AcceptInvalidAcertificates) in that order, leaving out nulls as the serializer options say. The SslCertificate
    /// property is never written or read: its getter loads the PFX file, and an X509 certificate cannot be represented
    /// as JSON (serializing settings with a PFX file configured failed before 10.2). Unknown properties are skipped.
    /// Thread safety: stateless and thread-safe.
    /// </summary>
    internal sealed class SslSettingsJsonConverter : JsonConverter<SslSettings>
    {
        #region Public-Members

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        /// <summary>
        /// Read SSL settings.
        /// </summary>
        /// <param name="reader">Reader positioned at the value.</param>
        /// <param name="typeToConvert">Type to convert.</param>
        /// <param name="options">Serializer options; property names honor PropertyNameCaseInsensitive.</param>
        /// <returns>SSL settings, or null for a JSON null.</returns>
        /// <exception cref="JsonException">Thrown when the value is not a JSON object or a field has the wrong type.</exception>
        public override SslSettings Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("SSL settings must be a JSON object.");

            StringComparison comparison = options.PropertyNameCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            SslSettings settings = new SslSettings();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject) return settings;
                if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Expected a property name in SSL settings.");

                string name = reader.GetString();
                reader.Read();

                if (String.Equals(name, nameof(SslSettings.Enable), comparison)) settings.Enable = reader.GetBoolean();
                else if (String.Equals(name, nameof(SslSettings.PfxCertificateFile), comparison)) settings.PfxCertificateFile = ReadString(ref reader);
                else if (String.Equals(name, nameof(SslSettings.PfxCertificatePassword), comparison)) settings.PfxCertificatePassword = ReadString(ref reader);
                else if (String.Equals(name, nameof(SslSettings.MutuallyAuthenticate), comparison)) settings.MutuallyAuthenticate = reader.GetBoolean();
                else if (String.Equals(name, nameof(SslSettings.AcceptInvalidAcertificates), comparison)) settings.AcceptInvalidAcertificates = reader.GetBoolean();
                else reader.Skip();
            }

            throw new JsonException("Unterminated SSL settings object.");
        }

        /// <summary>
        /// Write SSL settings.
        /// </summary>
        /// <param name="writer">Writer.</param>
        /// <param name="value">SSL settings; must not be null.</param>
        /// <param name="options">Serializer options; null strings are left out under JsonIgnoreCondition.WhenWritingNull.</param>
        public override void Write(Utf8JsonWriter writer, SslSettings value, JsonSerializerOptions options)
        {
            bool skipNulls = options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingNull;

            writer.WriteStartObject();
            writer.WriteBoolean(nameof(SslSettings.Enable), value.Enable);
            WriteString(writer, nameof(SslSettings.PfxCertificateFile), value.PfxCertificateFile, skipNulls);
            WriteString(writer, nameof(SslSettings.PfxCertificatePassword), value.PfxCertificatePassword, skipNulls);
            writer.WriteBoolean(nameof(SslSettings.MutuallyAuthenticate), value.MutuallyAuthenticate);
            writer.WriteBoolean(nameof(SslSettings.AcceptInvalidAcertificates), value.AcceptInvalidAcertificates);
            writer.WriteEndObject();
        }

        #endregion

        #region Private-Methods

        private static string ReadString(ref Utf8JsonReader reader)
        {
            return reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
        }

        private static void WriteString(Utf8JsonWriter writer, string name, string value, bool skipNulls)
        {
            if (value == null)
            {
                if (!skipNulls) writer.WriteNull(name);
                return;
            }

            writer.WriteString(name, value);
        }

        #endregion
    }
}
