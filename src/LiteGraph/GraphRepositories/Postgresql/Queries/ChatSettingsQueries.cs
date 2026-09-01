namespace LiteGraph.GraphRepositories.Postgresql.Queries
{
    using System;
    using System.Globalization;

    internal static class ChatSettingsQueries
    {
        internal static string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        internal static string Insert(ChatSettings settings)
        {
            string ret =
                "INSERT INTO 'chatsettings' "
                + "(tenantguid, defaultcompletionendpointguid, defaultembeddingendpointguid, systemprompt, "
                + "enablechat, enabletools, enablemutationtools, maxtooliterations, enablerag, ragtopk, "
                + "ragscorethreshold, historyretentiondays, createdutc, lastupdateutc) "
                + "VALUES ("
                + SqlString(settings.TenantGUID.ToString()) + ","
                + SqlString(settings.DefaultCompletionEndpointGUID != null ? settings.DefaultCompletionEndpointGUID.Value.ToString() : null) + ","
                + SqlString(settings.DefaultEmbeddingEndpointGUID != null ? settings.DefaultEmbeddingEndpointGUID.Value.ToString() : null) + ","
                + SqlString(settings.SystemPrompt) + ","
                + (settings.EnableChat ? "1" : "0") + ","
                + (settings.EnableTools ? "1" : "0") + ","
                + (settings.EnableMutationTools ? "1" : "0") + ","
                + settings.MaxToolIterations + ","
                + (settings.EnableRag ? "1" : "0") + ","
                + settings.RagTopK + ","
                + settings.RagScoreThreshold.ToString(CultureInfo.InvariantCulture) + ","
                + settings.HistoryRetentionDays + ","
                + SqlString(settings.CreatedUtc.ToString(TimestampFormat)) + ","
                + SqlString(settings.LastUpdateUtc.ToString(TimestampFormat))
                + ") "
                + "RETURNING *;";

            return ret;
        }

        internal static string SelectByTenant(Guid tenantGuid)
        {
            return "SELECT * FROM 'chatsettings' WHERE tenantguid = '" + tenantGuid + "';";
        }

        internal static string Update(ChatSettings settings)
        {
            return
                "UPDATE 'chatsettings' SET "
                + "lastupdateutc = '" + DateTime.UtcNow.ToString(TimestampFormat) + "',"
                + "defaultcompletionendpointguid = " + SqlString(settings.DefaultCompletionEndpointGUID != null ? settings.DefaultCompletionEndpointGUID.Value.ToString() : null) + ","
                + "defaultembeddingendpointguid = " + SqlString(settings.DefaultEmbeddingEndpointGUID != null ? settings.DefaultEmbeddingEndpointGUID.Value.ToString() : null) + ","
                + "systemprompt = " + SqlString(settings.SystemPrompt) + ","
                + "enablechat = " + (settings.EnableChat ? "1" : "0") + ","
                + "enabletools = " + (settings.EnableTools ? "1" : "0") + ","
                + "enablemutationtools = " + (settings.EnableMutationTools ? "1" : "0") + ","
                + "maxtooliterations = " + settings.MaxToolIterations + ","
                + "enablerag = " + (settings.EnableRag ? "1" : "0") + ","
                + "ragtopk = " + settings.RagTopK + ","
                + "ragscorethreshold = " + settings.RagScoreThreshold.ToString(CultureInfo.InvariantCulture) + ","
                + "historyretentiondays = " + settings.HistoryRetentionDays + " "
                + "WHERE tenantguid = '" + settings.TenantGUID + "' "
                + "RETURNING *;";
        }

        internal static string Delete(Guid tenantGuid)
        {
            return "DELETE FROM 'chatsettings' WHERE tenantguid = '" + tenantGuid + "';";
        }

        private static string SqlString(string val)
        {
            if (String.IsNullOrEmpty(val)) return "NULL";
            return "'" + Sanitizer.Sanitize(val) + "'";
        }
    }
}
