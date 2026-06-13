namespace LiteGraph.Sdk
{
    internal static class BulkCreateUrlHelper
    {
        internal static string AppendReturnMode(string url, BulkCreateReturnModeEnum returnMode)
        {
            if (returnMode != BulkCreateReturnModeEnum.Minimal) return url;
            return url + (url.Contains("?") ? "&" : "?") + "return=minimal";
        }
    }
}
