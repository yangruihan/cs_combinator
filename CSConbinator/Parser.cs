using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CSConbinator
{
    public delegate uint UserTokenCallback(string src, uint offset);

    public delegate Result<Tuple<bool, T>>
        VisitFuncDelegate<T>(AstNode ast, AstNode parent, int idx, T userData);

    public delegate Result<T> VisitFuncPostOrderDelegate<T>(AstNode ast, AstNode parent, int idx, List<T> userData);

    public static class Parser
    {
        public static Result<Token[]> Tokenize(string src, Tuple<string, string>[] rules)
        {
            var scanner = new Scanner();
            return scanner.Scan(src, rules.Select(rule => new TokenRule(rule.Item1, rule.Item2)).ToArray());
        }

        private static uint SkipWhitespace(string src, uint offset)
        {
            for (var i = offset; i < src.Length; i++)
            {
                if (src[(int) i] == '\n' || !char.IsWhiteSpace(src[(int) i]))
                {
                    return i;
                }
            }

            return (uint) src.Length;
        }

        public static Combinator UserToken(string symbol, UserTokenCallback callback)
        {
            var name = $"{Common.InnerSymbol}user_token[{symbol.ToReadable()}]";
            var info = $"{name}<{callback}>";
            var code = $"UserToken(\"{symbol.ToReadable()}\", null)";

            return new Combinator(
                name,
                info,
                code,
                (src, offset) =>
                {
                    offset = SkipWhitespace(src, offset);
                    var pos = (int) offset;

                    if (offset >= src.Length)
                    {
                        return Result<ParseCallbackRet>.Err(
                            new ParseUserTokenError(
                                $"parse user_token {name} failed, at '{src.SafeSubstring((int) offset, 30)}'"));
                    }

                    var len = callback(src, offset);
                    if (len > 0)
                    {
                        return Result<ParseCallbackRet>.Ok(new ParseCallbackRet
                        {
                            Lexeme = src.Substring((int) offset, (int) len),
                            Pos = pos,
                            Children = null,
                            Offset = offset + len
                        });
                    }

                    return Result<ParseCallbackRet>.Err(
                        new ParseUserTokenError(
                            $"parse user_token {name} failed, at '{src.SafeSubstring((int) offset, 30)}'"));
                });
        }

        public static Combinator NativeHandleToken(string symbol, string srcCode)
        {
            var name = $"{Common.InnerSymbol}native_handle_token[{symbol.ToReadable()}]";
            var info = $"{name}<native_handle_token>";
            var code = $"UserToken(\"{symbol.ToReadable()}\", \n{srcCode})";

            return new Combinator(
                name,
                info,
                code,
                (src, offset) => Result<ParseCallbackRet>.Err(
                    new ParseNativeHandleTokenError(
                        $"parse native_handle_token {name} failed, at '{src.SafeSubstring((int) offset, 30)}'")));
        }

        public static Combinator ReToken(string re)
        {
            var name = $"{Common.InnerSymbol}re_token";
            var info = $"re\"{re.ToReadable()}\"";
            var code = $"ReToken(@\"{re}\")";

            var regex = new Regex(re, RegexOptions.Compiled);

            return new Combinator(
                name,
                info,
                code,
                (src, offset) =>
                {
                    offset = SkipWhitespace(src, offset);
                    var pos = (int) offset;

                    var match = regex.Match(src, (int) offset);

                    if (match.Success && match.Index == offset)
                    {
                        return Result<ParseCallbackRet>.Ok(new ParseCallbackRet
                        {
                            Lexeme = src.Substring((int) offset, match.Length),
                            Pos = pos,
                            Children = null,
                            Offset = (uint) (offset + match.Length)
                        });
                    }

                    return Result<ParseCallbackRet>.Err(
                        new ParseReTokenError(
                            $"parse re_token {name} failed, at '{src.SafeSubstring((int) offset, 30)}'"));
                });
        }

        public static Combinator Token(string literal, Func<char, bool> sepCheckFunc = null,
            bool useDefaultSepCheck = false)
        {
            var name = $"{Common.InnerSymbol}token";
            var info = $"\"{literal.ToReadable()}\"";
            var code = useDefaultSepCheck
                ? $"Token(\"{literal.ToReadable()}\", null, true)"
                : $"Token(\"{literal.ToReadable()}\")";

            if (useDefaultSepCheck)
            {
                sepCheckFunc = Common.DefaultSepCheckFunc;
            }

            return new Combinator(
                name,
                info,
                code,
                (src, offset) =>
                {
                    offset = SkipWhitespace(src, offset);
                    var pos = (int) offset;

                    var subStr = src.SafeSubstring((int) offset, literal.Length);

                    if (subStr == literal)
                    {
                        if (sepCheckFunc != null)
                        {
                            if (sepCheckFunc(src[(int) (offset + literal.Length)]))
                            {
                                return Result<ParseCallbackRet>.Ok(new ParseCallbackRet
                                {
                                    Lexeme = literal,
                                    Pos = pos,
                                    Children = null,
                                    Offset = (uint) (offset + literal.Length)
                                });
                            }

                            return Result<ParseCallbackRet>.Err(new ParseTokenSepCheckFuncError(
                                $"parse token {name} failed, at '{src.SafeSubstring((int) offset, 30)}'"));
                        }

                        return Result<ParseCallbackRet>.Ok(new ParseCallbackRet
                        {
                            Lexeme = literal,
                            Pos = pos,
                            Children = null,
                            Offset = (uint) (offset + literal.Length)
                        });
                    }

                    return Result<ParseCallbackRet>.Err(new ParseTokenError(
                        $"parse token {name} failed, at '{src.SafeSubstring((int) offset, 30)}'"));
                });
        }

        public static Combinator Many(Combinator c)
        {
            var name = $"{Common.InnerSymbol}many";
            var info = $"({(c.Name.IsInnerSymbol() ? c.Info : c.Name)})*";
            var code = $"Many({c.Code})";

            return new Combinator(
                name,
                info,
                code,
                (src, offset) =>
                {
                    var pos = (int) offset;

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
                        Pos = pos,
                        Children = children.Count > 0 ? children : null,
                        Offset = offset
                    });
                });
        }

        public static Combinator Many1(Combinator c)
        {
            var name = $"{Common.InnerSymbol}many1";
            var info = $"({(c.Name.IsInnerSymbol() ? c.Info : c.Name)})+";
            var code = $"Many1({c.Code})";

            return new Combinator(
                name,
                info,
                code,
                (src, offset) =>
                {
                    var pos = (int) offset;
                    var ret = c.Parse(src, offset);

                    if (!ret.IsSuccess)
                    {
                        return Result<ParseCallbackRet>.Err(
                            new ParseMany1Error(
                                $"parse many1 {name} failed, at '{src.SafeSubstring((int) offset, 30)}'"));
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
                        Pos = pos,
                        Children = children.Count > 0 ? children : null,
                        Offset = offset
                    });
                });
        }

        public static Combinator Maybe(Combinator c)
        {
            var name = $"{Common.InnerSymbol}maybe";
            var info = $"({(c.Name.IsInnerSymbol() ? c.Info : c.Name)})?";
            var code = $"Maybe({c.Code})";

            return new Combinator(
                name,
                info,
                code,
                (src, offset) =>
                {
                    var pos = (int) offset;

                    var ret = c.Parse(src, offset);
                    var children = ret.IsSuccess && ret.Ret.AstNode != null
                        ? new List<AstNode> {ret.Ret.AstNode}
                        : null;

                    return Result<ParseCallbackRet>.Ok(new ParseCallbackRet
                    {
                        Lexeme = children == null ? "" : children[0].Lexeme,
                        Pos = pos,
                        Children = children,
                        Offset = ret.IsSuccess ? ret.Ret.Offset : offset
                    });
                });
        }

        public static Combinator Group(Combinator c)
        {
            var name = $"{Common.InnerSymbol}group";
            var info = $"( {(c.Name.IsInnerSymbol() ? c.Info : c.Name)} )";
            var code = $"Group({c.Code})";

            return new Combinator(
                name,
                info,
                code,
                (src, offset) =>
                {
                    var pos = (int) offset;

                    var ret = c.Parse(src, offset);
                    if (ret.IsSuccess)
                    {
                        return Result<ParseCallbackRet>.Ok(new ParseCallbackRet
                        {
                            Lexeme = ret.Ret.AstNode.Lexeme,
                            Pos = pos,
                            Children = new List<AstNode> {ret.Ret.AstNode},
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
                "EOF",
                (src, offset) =>
                {
                    if (offset >= src.Length)
                    {
                        return Result<ParseCallbackRet>.Ok(new ParseCallbackRet
                        {
                            Lexeme = $"{Common.InnerSymbol}eof",
                            Pos = 0,
                            Children = null,
                            Offset = offset
                        });
                    }

                    return Result<ParseCallbackRet>.Err(
                        new ParseMany1Error(
                            $"parse eof failed, at '{src.SafeSubstring((int) offset, 30)}'"));
                });
        }

        public static Result<AstNode> Parse(string src, Combinator c)
        {
            var ret = c.Parse(src, 0);
            return ret.IsSuccess ? Result<AstNode>.Ok(ret.Ret.AstNode) : Result<AstNode>.Err(ret.Error);
        }

        public static Result<List<T>> VisitAst<T>(AstNode ast, VisitFuncDelegate<T> visitFunc, T userData,
            AstNode parent = null,
            int idx = 0)
        {
            var ret = visitFunc(ast, parent, idx, userData);

            if (!ret.IsSuccess)
            {
                return Result<List<T>>.Err(ret.Error);
            }

            if (ast.Children.IsNullOrEmpty()) return Result<List<T>>.Ok(new List<T> {ret.Ret.Item2});

            var userDataList = new List<T>();
            for (var i = 0; i < ast.Children.Count; i++)
            {
                var astChild = ast.Children[i];
                var visitRet = VisitAst(astChild, visitFunc, userData, ast, i);

                if (!visitRet.IsSuccess)
                {
                    return Result<List<T>>.Err(visitRet.Error);
                }

                userDataList.AddRange(visitRet.Ret);
            }

            return Result<List<T>>.Ok(userDataList);
        }

        public static Result<T> VisitAstPostOrder<T>(AstNode ast, VisitFuncPostOrderDelegate<T> visitFunc,
            T userData = default,
            AstNode parent = null, int idx = 0)
        {
            var userDataList = new List<T>();

            if (!ast.Children.IsNullOrEmpty())
            {
                for (var i = 0; i < ast.Children.Count; i++)
                {
                    var astChild = ast.Children[i];
                    var visitRet = VisitAstPostOrder(astChild, visitFunc, userData, ast, i);

                    if (!visitRet.IsSuccess)
                    {
                        return Result<T>.Err(visitRet.Error);
                    }

                    userDataList.Add(visitRet.Ret);
                }
            }

            return visitFunc(ast, parent, idx, userDataList);
        }

        public static string AstStr(AstNode ast, int indent = 0, string filename = "", string src = "")
        {
            void InnerAstStr(AstNode node, int i, StringBuilder sb)
            {
                sb.Append(string.Concat(Enumerable.Repeat(" ", i)));
                if (!string.IsNullOrEmpty(filename) && !string.IsNullOrEmpty(src))
                {
                    sb.AppendLine(node.ToStringWithPos(filename, src));
                }
                else
                {
                    sb.AppendLine(node.ToString());
                }

                if (node.Children.IsNullOrEmpty()) return;

                foreach (var astChild in node.Children)
                {
                    InnerAstStr(astChild, i + 4, sb);
                }
            }

            var stringBuilder = new StringBuilder();
            InnerAstStr(ast, indent, stringBuilder);

            return stringBuilder.ToString();
        }

        public static string GrammarStr(this Grammar g, string[] order = null)
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

        public static string GrammarCodeStr(this Grammar g, string[] order = null, string grammarName = "_g")
        {
            if (order == null)
            {
                return g.ToCodeString(grammarName);
            }

            var sb = new StringBuilder();

            foreach (var o in order)
            {
                if (o == "EOF")
                {
                    continue;
                }

                var value = g[o];
                sb.AppendLine($"{grammarName}[\"{o}\"] = {Grammar.ReplaceTempSymbolName(value.Code, grammarName)};\n");
            }

            sb.AppendLine($"{grammarName}[\"EOF\"] = Eof();");

            return sb.ToString();
        }
    }
}