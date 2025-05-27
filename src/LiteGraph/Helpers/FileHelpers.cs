namespace LiteGraph.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// File helpers.
    /// </summary>
    public static class FileHelpers
    {
        /// <summary>
        /// Normalize a directory.
        /// </summary>
        /// <param name="directory">Directory.</param>
        /// <returns>Directory.</returns>
        public static string NormalizeDirectory(string directory)
        {
            if (String.IsNullOrEmpty(directory)) return directory;
            while (directory.Contains("\\")) directory = directory.Replace("\\", "/");
            while (directory.Contains("..")) directory = directory.Replace("..", "");
            while (directory.Contains("//")) directory = directory.Replace("//", "/");
            if (!directory.EndsWith("/")) directory += "/";
            if (directory.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                throw new ArgumentException("The specified directory contains invalid characters.");
            return directory;
        }
    }
}
