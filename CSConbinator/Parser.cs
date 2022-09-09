using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CSConbinator
{
    public delegate uint UserTokenCallback(string src, uint offset);

    public delegate Tuple<bool, object> VisitFuncDelegate(AstNode ast, AstNode parent, int idx, object userData);

    public delegate object VisitFuncPostOrderDelegate(AstNode ast, AstNode parent, int idx, object userData);

    public class Parser
    {
        private static readonly Dictionary<string, string> EscapeMap = new Dictionary<string, string>
        {
            { "\\n", "\n" },
            { "\\r", "\r" },
            { "\\t", "\t" },
            { "\\v", "\v" },
            { "\\\\", "\\" },
            { "\\'", "'" },
            { "\\\"", "\"" },
            { "\\0", "\0" }
        };

        private static readonly Dictionary<string, string> EscapeMap2 = new Dictionary<string, string>
        {
            { "\n", "\\n" },
            { "\r", "\\r" },
            { "\t", "\\t" },
            { "\v", "\\v" },
            { "\\", "\\\\" },
            { "'", "\\'" },
            { "\"", "\\\"" },
            { "\0", "\\0" }
        };

        public static Result<Token[]> Tokenize(string src, Tuple<string, string>[] rules)
        {
            var scanner = new Scanner();
            return scanner.Scan(src, rules.Select(rule => new TokenRule(rule.Item1, rule.Item2)).ToArray());
        }

        private static uint SkipWhitespace(string src, uint offset)
        {
            for (var i = offset; i < src.Length; i++)
            {
                if (src[(int)i] == '\n' || !char.IsWhiteSpace(src[(int)i]))
                {
                    return i;
                }
            }

            return (uint)src.Length;
        }

        public static Combinator UserToken(string _, UserTokenCallback callback)
        {
            var name = $"{Common.InnerSymbol}user_token";
            return new Combinator(
                name,
                $"{name}<{callback}>",
                (src, offset) =>
                {
                    offset = SkipWhitespace(src, offset);

                    var len = callback(src, offset);

                    if (len > 0)
                    {
                        return Result<ParseCallbackRet>.Ok(new ParseCallbackRet
                        {
                            Lexeme = src.Substring((int)offset, (int)len),
                            Children = null,
                            Offset = offset + len
                        });
                    }

                    return Result<ParseCallbackRet>.Err(
                        new ParseUserTokenError(
                            $"parse user_token {name} failed, at '{src.SafeSubstring((int)offset, 30)}'"));
                });
        }

        public static Combinator ReToken(string re)
        {
            var name = $"{Common.InnerSymbol}re_token";
            var info = $"re\"{re}\"";

            var regex = new Regex(re, RegexOptions.Compiled);

            return new Combinator(
                name,
                info,
                (src, offset) =>
                {
                    offset = SkipWhitespace(src, offset);

                    var match = regex.Match(src, (int)offset);

                    if (match.Success && match.Index == offset)
                    {
                        return Result<ParseCallbackRet>.Ok(new ParseCallbackRet
                        {
                            Lexeme = src.Substring((int)offset, match.Length),
                            Children = null,
                            Offset = (uint)(offset + match.Length)
                        });
                    }

                    return Result<ParseCallbackRet>.Err(
                        new ParseReTokenError(
                            $"parse re_token {name} failed, at '{src.SafeSubstring((int)offset, 30)}'"));
                });
        }

        public static Combinator Token(string literal, Func<char, bool> sepCheckFunc = null)
        {
            var name = $"{Common.InnerSymbol}token";
            var info = $"\"{literal}\"";

            if (EscapeMap2.TryGetValue(literal, out var s))
            {
                info = $"\"{s}\"";
            }

            return new Combinator(
                name,
                info,
                (src, offset) =>
                {
                    offset = SkipWhitespace(src, offset);

                    var subStr = src.SafeSubstring((int)offset, literal.Length);

                    if (subStr == literal)
                    {
                        if (sepCheckFunc != null)
                        {
                            if (sepCheckFunc(src[(int)(offset + literal.Length)]))
                            {
                                return Result<ParseCallbackRet>.Ok(new ParseCallbackRet
                                {
                                    Lexeme = literal,
                                    Children = null,
                                    Offset = (uint)(offset + literal.Length)
                                });
                            }

                            return Result<ParseCallbackRet>.Err(new ParseTokenSepCheckFuncError(
                                $"parse token {name} failed, at '{src.SafeSubstring((int)offset, 30)}'"));
                        }

                        return Result<ParseCallbackRet>.Ok(new ParseCallbackRet
                        {
                            Lexeme = literal,
                            Children = null,
                            Offset = (uint)(offset + literal.Length)
                        });
                    }

                    return Result<ParseCallbackRet>.Err(new ParseTokenError(
                        $"parse token {name} failed, at '{src.SafeSubstring((int)offset, 30)}'"));
                });
        }

        public static Combinator Many(Combinator c)
        {
            var name = $"{Common.InnerSymbol}many";
            var info = $"({(c.Name.IsInnerSymbol() ? c.Info : c.Name)})*";

            return new Combinator(
                name,
                info,
                (src, offset) =>
                {
                    var ret = c.Parse(src, offset);

                    var children = new List<AstNode>();
                    var lexemes = new List<string>();

                    while (ret.IsSuccess)
                    {
                        if (ret.Ret.AstNode != null)
                        {
                            children.Add(ret.Ret.AstNode);
                            lexemes.Add(ret.Ret.AstNode.Lexeme);
                        }

                        offset = ret.Ret.Offset;
                        ret = c.Parse(src, offset);
                    }

                    return Result<ParseCallbackRet>.Ok(new ParseCallbackRet
                    {
                        Lexeme = Common.LexemeConcat(lexemes),
                        Children = children.Count > 0 ? children : null,
                        Offset = offset
                    });
                });
        }

        public static Combinator Many1(Combinator c)
        {
            var name = $"{Common.InnerSymbol}many1";
            var info = $"({(c.Name.IsInnerSymbol() ? c.Info : c.Name)})+";

            return new Combinator(
                name,
                info,
                (src, offset) =>
                {
                    var ret = c.Parse(src, offset);

                    if (!ret.IsSuccess)
                    {
                        return Result<ParseCallbackRet>.Err(
                            new ParseMany1Error(
                                $"parse many1 {name} failed, at '{src.SafeSubstring((int)offset, 30)}'"));
                    }

                    var children = new List<AstNode>();
                    var lexemes = new List<string>();

                    while (ret.IsSuccess)
                    {
                        if (ret.Ret.AstNode != null)
                        {
                            children.Add(ret.Ret.AstNode);
                            lexemes.Add(ret.Ret.AstNode.Lexeme);
                        }

                        offset = ret.Ret.Offset;
                        ret = c.Parse(src, offset);
                    }

                    return Result<ParseCallbackRet>.Ok(new ParseCallbackRet
                    {
                        Lexeme = Common.LexemeConcat(lexemes),
                        Children = children.Count > 0 ? children : null,
                        Offset = offset
                    });
                });
        }

        public static Combinator Maybe(Combinator c)
        {
            var name = $"{Common.InnerSymbol}maybe";
            var info = $"({(c.Name.IsInnerSymbol() ? c.Info : c.Name)})?";

            return new Combinator(
                name,
                info,
                (src, offset) =>
                {
                    var ret = c.Parse(src, offset);
                    var children = ret.IsSuccess && ret.Ret.AstNode != null
                        ? new List<AstNode> { ret.Ret.AstNode }
                        : null;

                    return Result<ParseCallbackRet>.Ok(new ParseCallbackRet
                    {
                        Lexeme = children == null ? "" : children[0].Lexeme,
                        Children = children,
                        Offset = ret.IsSuccess ? ret.Ret.Offset : offset
                    });
                });
        }

        public static Combinator Group(Combinator c)
        {
            var name = $"{Common.InnerSymbol}group";
            var info = $"( {(c.Name.IsInnerSymbol() ? c.Info : c.Name)} )";

            return new Combinator(
                name,
                info,
                (src, offset) =>
                {
                    var ret = c.Parse(src, offset);
                    if (ret.IsSuccess)
                    {
                        return Result<ParseCallbackRet>.Ok(new ParseCallbackRet
                        {
                            Lexeme = ret.Ret.AstNode.Lexeme,
                            Children = new List<AstNode> { ret.Ret.AstNode },
                            Offset = ret.Ret.Offset
                        });
                    }

                    return Result<ParseCallbackRet>.Err(ret.Error);
                });
        }

        public static Combinator Eof()
        {
            return new Combinator(
                $"{Common.InnerSymbol}eof",
                "EOF",
                (src, offset) =>
                {
                    if (offset >= src.Length)
                    {
                        return Result<ParseCallbackRet>.Ok(new ParseCallbackRet
                        {
                            Lexeme = $"{Common.InnerSymbol}eof",
                            Children = null,
                            Offset = offset
                        });
                    }

                    return Result<ParseCallbackRet>.Err(
                        new ParseMany1Error(
                            $"parse eof failed, at '{src.SafeSubstring((int)offset, 30)}'"));
                });
        }

        public static Result<AstNode> Parse(string src, Combinator c)
        {
            var ret = c.Parse(src, 0);
            return ret.IsSuccess ? Result<AstNode>.Ok(ret.Ret.AstNode) : Result<AstNode>.Err(ret.Error);
        }

        public static List<object> VisitAst(AstNode ast, VisitFuncDelegate visitFunc, object userData,
            AstNode parent = null,
            int idx = 0)
        {
            var ret = visitFunc(ast, parent, idx, userData);

            if (ret.Item1 && !ast.Children.IsNullOrEmpty())
            {
                var userDataList = new List<object>();
                for (var i = 0; i < ast.Children.Count; i++)
                {
                    var astChild = ast.Children[i];
                    var visitRet = VisitAst(astChild, visitFunc, userData, ast, i);
                    userDataList.AddRange(visitRet);
                }

                return userDataList;
            }

            return new List<object> { ret.Item2 };
        }

        public static object VisitAstPostOrder(AstNode ast, VisitFuncPostOrderDelegate visitFunc, object userData,
            AstNode parent = null, int idx = 0)
        {
            var userDataList = new List<object>();

            if (!ast.Children.IsNullOrEmpty())
            {
                for (var i = 0; i < ast.Children.Count; i++)
                {
                    var astChild = ast.Children[i];
                    var visitRet = VisitAstPostOrder(astChild, visitFunc, userData, ast, i);
                    userDataList.Add(visitRet);
                }
            }

            return visitFunc(ast, parent, idx, userDataList);
        }

        public static string AstStr(AstNode ast, int indent = 0)
        {
            void InnerAstStr(AstNode node, int i, StringBuilder sb)
            {
                sb.Append(string.Concat(Enumerable.Repeat(" ", i)));
                sb.AppendLine(node.ToString());

                if (!node.Children.IsNullOrEmpty())
                {
                    foreach (var astChild in node.Children)
                    {
                        InnerAstStr(astChild, i + 4, sb);
                    }
                }
            }

            var stringBuilder = new StringBuilder();
            InnerAstStr(ast, indent, stringBuilder);

            return stringBuilder.ToString();
        }

        public static string GrammarStr(Grammar g, string[] order = null)
        {
            if (order == null)
            {
                return g.ToString();
            }

            var sb = new StringBuilder();

            foreach (var o in order)
            {
                var value = g[o];
                sb.AppendLine($"{o} = {value.Info};\n");
            }

            return sb.ToString();
        }
    }
}