namespace LiteGraph.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// Hash methods.
    /// </summary>
    public static class HashHelper
    {
        /// <summary>
        /// Generate an MD5 hash.
        /// </summary>
        /// <param name="data">Data.</param>
        /// <returns>MD5 hash.</returns>
        public static byte[] MD5Hash(byte[] data)
        {
            if (data == null || data.Length < 1) data = Array.Empty<byte>();
            using (MD5 hash = MD5.Create())
            {
                return hash.ComputeHash(data);
            }
        }

        /// <summary>
        /// Generate an MD5 hash.
        /// </summary>
        /// <param name="stream">Stream.</param>
        /// <returns>MD5 hash.</returns>
        public static byte[] MD5Hash(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead) throw new ArgumentException("Unable to read from supplied stream.");
            if (!stream.CanSeek) throw new ArgumentException("Unable to seek in supplied stream.");

            stream.Seek(0, SeekOrigin.Begin);

            using (MD5 md5 = MD5.Create())
            {
                return md5.ComputeHash(stream);
            }
        }

        /// <summary>
        /// Generate an MD5 hash of a string.
        /// </summary>
        /// <param name="str">String.</param>
        /// <returns>MD5 hash.</returns>
        public static byte[] MD5Hash(string str)
        {
            if (String.IsNullOrEmpty(str)) str = "";
            return MD5Hash(Encoding.UTF8.GetBytes(str));
        }

        /// <summary>
        /// Generate an MD5 hash of a list of strings.
        /// </summary>
        /// <param name="strings">Strings.</param>
        /// <returns>MD5 hash.</returns>
        public static byte[] MD5Hash(List<string> strings)
        {
            if (strings == null || strings.Count < 1) return Array.Empty<byte>();
            string concatenated = string.Join("\0", strings);
            return MD5Hash(concatenated);
        }

        /// <summary>
        /// Generate an MD5 hash of a DataTable.
        /// This method concatenates column names (separated by a null character) and all cell values (separated by a null character).  Any null cells have their value replaced with the string NULL.
        /// </summary>
        /// <param name="dt">DataTable. </param>
        /// <returns>MD5 hash.</returns>
        public static byte[] MD5Hash(DataTable dt)
        {
            if (dt == null) return MD5Hash("");
            return MD5Hash(DataTableToString(dt));
        }

        /// <summary>
        /// Generate a SHA1 hash of a byte array.
        /// </summary>
        /// <param name="data">Data.</param>
        /// <returns>SHA1 hash.</returns>
        public static byte[] SHA1Hash(byte[] data)
        {
            if (data == null || data.Length < 1) data = Array.Empty<byte>();
            using (SHA1 hash = SHA1.Create())
            {
                return hash.ComputeHash(data);
            }
        }

        /// <summary>
        /// Generate a SHA1 hash.
        /// </summary>
        /// <param name="stream">Stream.</param>
        /// <returns>MD5 hash.</returns>
        public static byte[] SHA1Hash(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead) throw new ArgumentException("Unable to read from supplied stream.");
            if (!stream.CanSeek) throw new ArgumentException("Unable to seek in supplied stream.");

            stream.Seek(0, SeekOrigin.Begin);

            using (SHA1 sha1 = SHA1.Create())
            {
                return sha1.ComputeHash(stream);
            }
        }

        /// <summary>
        /// Generate a SHA1 hash of a string.
        /// </summary>
        /// <param name="str">String.</param>
        /// <returns>SHA1 hash.</returns>
        public static byte[] SHA1Hash(string str)
        {
            if (String.IsNullOrEmpty(str)) str = "";
            return SHA1Hash(Encoding.UTF8.GetBytes(str));
        }

        /// <summary>
        /// Generate a SHA1 hash of a list of strings.
        /// </summary>
        /// <param name="strings">Strings.</param>
        /// <returns>SHA1 hash.</returns>
        public static byte[] SHA1Hash(List<string> strings)
        {
            if (strings == null || strings.Count < 1) return Array.Empty<byte>();
            string concatenated = string.Join("\0", strings);
            return SHA1Hash(concatenated);
        }

        /// <summary>
        /// Generate a SHA1 hash of a DataTable.
        /// This method concatenates column names (separated by a null character) and all cell values (separated by a null character).  Any null cells have their value replaced with the string NULL.
        /// </summary>
        /// <param name="dt">DataTable. </param>
        /// <returns>SHA1 hash.</returns>
        public static byte[] SHA1Hash(DataTable dt)
        {
            if (dt == null) return SHA1Hash("");
            return SHA1Hash(DataTableToString(dt));
        }

        /// <summary>
        /// Generate a SHA256 hash of a byte array.
        /// </summary>
        /// <param name="data">Data.</param>
        /// <returns>SHA256 hash.</returns>
        public static byte[] SHA256Hash(byte[] data)
        {
            if (data == null || data.Length < 1) data = Array.Empty<byte>();
            using (SHA256 hash = SHA256.Create())
            {
                return hash.ComputeHash(data);
            }
        }

        /// <summary>
        /// Generate a SHA256 hash.
        /// </summary>
        /// <param name="stream">Stream.</param>
        /// <returns>MD5 hash.</returns>
        public static byte[] SHA256Hash(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead) throw new ArgumentException("Unable to read from supplied stream.");
            if (!stream.CanSeek) throw new ArgumentException("Unable to seek in supplied stream.");

            stream.Seek(0, SeekOrigin.Begin);

            using (SHA256 sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(stream);
            }
        }

        /// <summary>
        /// Generate a SHA256 hash of a string.
        /// </summary>
        /// <param name="str">String.</param>
        /// <returns>SHA256 hash.</returns>
        public static byte[] SHA256Hash(string str)
        {
            if (String.IsNullOrEmpty(str)) str = "";
            return SHA256Hash(Encoding.UTF8.GetBytes(str));
        }

        /// <summary>
        /// Generate a SHA256 hash of a list of strings.
        /// </summary>
        /// <param name="strings">Strings.</param>
        /// <returns>SHA256 hash.</returns>
        public static byte[] SHA256Hash(List<string> strings)
        {
            if (strings == null || strings.Count < 1) return Array.Empty<byte>();
            string concatenated = string.Join("\0", strings);
            return SHA256Hash(concatenated);
        }

        /// <summary>
        /// Generate a SHA256 hash of a DataTable.
        /// This method concatenates column names (separated by a null character) and all cell values (separated by a null character).  Any null cells have their value replaced with the string NULL.
        /// </summary>
        /// <param name="dt">DataTable. </param>
        /// <returns>SHA256 hash.</returns>
        public static byte[] SHA256Hash(DataTable dt)
        {
            if (dt == null) return SHA256Hash("");
            return SHA256Hash(DataTableToString(dt));
        }

        private static string DataTableToString(DataTable dt)
        {
            StringBuilder sb = new StringBuilder();

            foreach (DataColumn col in dt.Columns)
            {
                sb.Append(col.ColumnName).Append('\0');
            }

            foreach (DataRow row in dt.Rows)
            {
                foreach (var item in row.ItemArray)
                {
                    // Convert to string, handling nulls
                    string value = item?.ToString() ?? "NULL";
                    sb.Append(value).Append('\0');
                }
            }

            return sb.ToString();
        }
    }
}
