namespace LiteGraph.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// String collection helpers.
    /// </summary>
    public static class StringHelpers
    {
        /// <summary>
        /// Combine two string lists.
        /// </summary>
        /// <param name="list1">String list 1.</param>
        /// <param name="list2">String list 2.</param>
        /// <returns>String list.</returns>
        public static List<string> Combine(List<string> list1, List<string> list2)
        {
            if (list1 == null) return list2;
            if (list2 == null) return list1;
            List<string> ret = new List<string>();
            ret.AddRange(list1);
            ret.AddRange(list2);
            return ret;
        }

        /// <summary>
        /// Redact a string.
        /// </summary>
        /// <param name="str">String.</param>
        /// <param name="replacementChar">Replacement character.</param>
        /// <param name="charsToRetain">Number of characters to retain.</param>
        /// <returns>Redacted string.</returns>
        public static string RedactTail(string str, string replacementChar = "*", int charsToRetain = 4)
        {
            if (String.IsNullOrEmpty(str)) return str;
            if (String.IsNullOrEmpty(replacementChar)) throw new ArgumentNullException(nameof(replacementChar));
            if (charsToRetain < 0) throw new ArgumentOutOfRangeException(nameof(charsToRetain));

            if (str.Length < charsToRetain)
            {
                return new string(replacementChar[0], str.Length);
            }
            else
            {
                return str.Substring(0, charsToRetain) + new string(replacementChar[0], str.Length - charsToRetain);
            }
        }

        /// <summary>
        /// Convert a CSV string list of GUIDs to a List of Guid.
        /// </summary>
        /// <param name="str">Input string.</param>
        /// <returns>List of GUIDs.</returns>
        public static List<Guid> StringToGuidList(string str)
        {
            if (String.IsNullOrEmpty(str)) return null;
            string[] parts = str.Split(',');

            List<Guid> ret = new List<Guid>();

            foreach (string curr in parts)
            {
                if (Guid.TryParse(curr, out Guid guid)) ret.Add(guid);
            }

            return ret;
        }
    }
}
