namespace LiteGraph.GraphRepositories.Postgresql
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;

    internal static class Sanitizer
    {
        internal static string Sanitize(string val)
        {
            if (String.IsNullOrEmpty(val)) return val;

            string ret = "";

            //
            // null, below ASCII range, above ASCII range
            //
            for (int i = 0; i < val.Length; i++)
            {
                if (((int)(val[i]) == 10) ||      // Preserve carriage return
                    ((int)(val[i]) == 13))        // and line feed
                {
                    ret += val[i];
                }
                else if ((int)(val[i]) < 32)
                {
                    continue;
                }
                else
                {
                    ret += val[i];
                }
            }

            //
            // in-string replacement
            //
            // Quote doubling is the complete defense for values embedded in
            // single-quoted literals; comment tokens (--, /*, */) are inert
            // inside a quoted string and must be preserved so stored content
            // (markdown tables, code, names) survives round-trips intact.
            //
            ret = ret.Replace("'", "''");
            return ret;
        }

        internal static string SanitizeJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return json;

            try
            {
                JsonDocument.Parse(json);
            }
            catch (JsonException e)
            {
                throw new ArgumentException("Invalid JSON provided for data.", nameof(json), e);
            }

            return json.Replace("'", "''");
        }
    }
}

