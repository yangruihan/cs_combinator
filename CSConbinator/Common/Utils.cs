using System;

namespace CSConbinator
{
    public static class StrUtils
    {
        public static string SafeSubstring(this string src, int startIdx, int len)
        {
            len = Math.Min(src.Length - startIdx, len);
            return src.Substring(startIdx, len);
        }

        public static string Truncate(this string src, int len, string append = "...")
        {
            return src.Length > len ? $"{src.Substring(0, len)}{append}" : src;
        }
    }
}