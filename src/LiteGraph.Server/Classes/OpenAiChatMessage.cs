namespace LiteGraph.Server.Classes
{
    using System;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// OpenAI-compatible chat message with a role and content.
    /// Content is accepted either as a string or as an array of typed content parts.
    /// </summary>
    public class OpenAiChatMessage
    {
        #region Public-Members

        /// <summary>
        /// Role, for example system, user, or assistant.
        /// </summary>
        [JsonPropertyName("role")]
        public string Role { get; set; } = null;

        /// <summary>
        /// Content.  Either a JSON string or an array of content parts with text fields.  Null when absent.
        /// </summary>
        [JsonPropertyName("content")]
        public JsonElement? Content { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public OpenAiChatMessage()
        {

        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Extract the plain-text content of the message.  String content is returned verbatim; array
        /// content concatenates the text fields of its parts.  Returns null when no text is present.
        /// </summary>
        /// <returns>Plain-text content, or null.</returns>
        public string GetContentText()
        {
            if (Content == null) return null;

            JsonElement element = Content.Value;

            if (element.ValueKind == JsonValueKind.String) return element.GetString();

            if (element.ValueKind == JsonValueKind.Array)
            {
                StringBuilder sb = new StringBuilder();

                foreach (JsonElement part in element.EnumerateArray())
                {
                    if (part.ValueKind == JsonValueKind.String)
                    {
                        sb.Append(part.GetString());
                    }
                    else if (part.ValueKind == JsonValueKind.Object
                        && part.TryGetProperty("text", out JsonElement text)
                        && text.ValueKind == JsonValueKind.String)
                    {
                        sb.Append(text.GetString());
                    }
                }

                return (sb.Length > 0 ? sb.ToString() : null);
            }

            return null;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
