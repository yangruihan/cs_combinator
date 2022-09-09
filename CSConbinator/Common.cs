using System.Collections.Generic;
using System.Linq;

namespace CSConbinator
{
    internal static class Common
    {
        internal static readonly string InnerSymbol = "@_@";

        public static bool IsInnerSymbol(this string s)
        {
            return s.StartsWith(InnerSymbol);
        }

        public static string LexemeConcat(List<string> lexemes)
        {
            if (lexemes == null || lexemes.Count == 0)
            {
                return "";
            }

            return string.Join(" ", lexemes.Where(l => !string.IsNullOrEmpty(l)));
        }

        public static List<AstNode> ReplaceInnerAstNode(List<AstNode> children)
        {
            while (true)
            {
                if (children == null)
                {
                    return null;
                }

                if (children.Count == 1 && children[0].Type.IsInnerSymbol())
                {
                    children = children[0].Children;
                    continue;
                }

                if (children.Count > 1)
                {
                    var newChildren = new List<AstNode>();

                    foreach (var child in children)
                    {
                        if (child.Type.IsInnerSymbol())
                        {
                            var expandChild = ReplaceInnerAstNode(child.Children);
                            if (expandChild != null)
                            {
                                newChildren.AddRange(expandChild);
                            }
                        }
                        else
                        {
                            newChildren.Add(child);
                        }
                    }

                    children = newChildren;
                }

                return children;
            }
        }

        public static bool IsNullOrEmpty(this List<AstNode> astNodes)
        {
            return astNodes == null || astNodes.Count == 0;
        }

        public static readonly Dictionary<string, string> EscapeMap = new Dictionary<string, string>
        {
            { "\\\\", "\\" },
            { "\\n", "\n" },
            { "\\r", "\r" },
            { "\\t", "\t" },
            { "\\v", "\v" },
            { "\\'", "'" },
            { "\\\"", "\"" },
            { "\\0", "\0" }
        };

        public static readonly Dictionary<string, string> EscapeMap2 = new Dictionary<string, string>
        {
            { "\\", "\\\\" },
            { "\n", "\\n" },
            { "\r", "\\r" },
            { "\t", "\\t" },
            { "\v", "\\v" },
            { "'", "\\'" },
            { "\"", "\\\"" },
            { "\0", "\\0" }
        };

        public static string ToRaw(this string s)
        {
            return EscapeMap.Aggregate(s, (current, pair) => current.Replace(pair.Key, pair.Value));
        }

        public static string ToReadable(this string s)
        {
            return EscapeMap2.Aggregate(s, (current, pair) => current.Replace(pair.Key, pair.Value));
        }

        public static bool DefaultSepCheckFunc(char c)
        {
            return char.IsWhiteSpace(c);
        }
    }
}