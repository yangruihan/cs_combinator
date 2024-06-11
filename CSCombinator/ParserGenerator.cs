using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static CSCombinator.GrammarParser;

namespace CSCombinator
{
    public static class ParserGenerator
    {
        private static readonly string Template = @"
// ---------------
// Auto Generated
// ---------------

using CSCombinator;

using static CSCombinator.Parser;

public class ${ClassName}: IParser
{
    private readonly Grammar _g = new Grammar();

    public Grammar Grammar => _g;

    private readonly string[] _productList =
    {
        ${ProductList}
    };

    public ${ClassName}()
    {
        ${GrammarBuild}
    }

    public Result<AstNode> Parse(string source)
    {
        return Parser.Parse(source, _g[_productList[0]]);
    }

    public string GrammarString()
    {
        return ToString();
    }

    public string GrammarCodeString()
    {
        return _g.GrammarCodeStr(_productList);
    }

    public override string ToString()
    {
        return $""Grammar:\n{_g.GrammarStr(_productList)}"";
    }
}";

        public static Result<string> Gen(string grammarSrc, string className)
        {
            var parser = new GrammarParser();
            var parseRet = parser.Parse(grammarSrc);
            if (!parseRet.IsSuccess)
            {
                return Result<string>.Err(parseRet.Error);
            }

            var rootAst = parseRet.Ret;

            if (rootAst.Children.IsNullOrEmpty())
            {
                return Result<string>.Err(new NoProductFoundError($"no product found"));
            }

            // remove EOL between product
            for (var i = rootAst.Children.Count - 1; i >= 0; i--)
            {
                if (rootAst.Children[i].IsEOL())
                {
                    rootAst.Children.RemoveAt(i);
                }
            }

            var productList = new List<string>();

            var tokenRegex = new Regex(@"^[A-Za-z_][\w]*", RegexOptions.Compiled);

            var grammar = new Grammar();

            Result<Combinator> Handle(AstNode ast, AstNode parent, int idx, List<Combinator> combs)
            {
                switch (ast.Type)
                {
                    case Products:
                    {
                        return Result<Combinator>.Ok(null);
                    }

                    case Product:
                    {
                        var symbol = ast.Children[0].Lexeme;

                        grammar[symbol] = combs[1];

                        // record product
                        productList.Add(symbol);

                        return Result<Combinator>.Ok(null);
                    }

                    case Expr:
                    {
                        var c = combs[0];
                        for (var i = 1; i < combs.Count; i++)
                        {
                            if (!ast.Children[i].IsEOL())
                            {
                                c += combs[i];
                            }
                        }

                        return Result<Combinator>.Ok(c);
                    }

                    case OrExpr:
                    {
                        if (combs.Count > 1)
                        {
                            var c = combs[0];
                            for (var i = 1; i < combs.Count; i++)
                            {
                                if (!ast.Children[i].IsEOL())
                                {
                                    c |= combs[i];
                                }
                            }

                            return Result<Combinator>.Ok(c);
                        }

                        return Result<Combinator>.Ok(combs[0]);
                    }

                    case Many:
                    {
                        if (combs.Count > 1)
                        {
                            var c = combs[0];
                            var symbol = ast.Children[1].Lexeme;

                            switch (symbol)
                            {
                                case "*":
                                    return Result<Combinator>.Ok(Parser.Many(c));
                                case "+":
                                    return Result<Combinator>.Ok(Parser.Many1(c));
                                case "?":
                                    return Result<Combinator>.Ok(Parser.Maybe(c));
                            }

                            return Result<Combinator>.Err(new GenError($"invalid symbol {symbol}"));
                        }

                        return Result<Combinator>.Ok(combs[0]);
                    }

                    case Paren:
                    {
                        if (combs.Count > 1)
                        {
                            var c = combs[0];
                            for (var i = 1; i < combs.Count; i++)
                            {
                                if (!ast.Children[i].IsEOL())
                                {
                                    c += combs[i];
                                }
                            }

                            return Result<Combinator>.Ok(Parser.Group(c));
                        }

                        return Result<Combinator>.Ok(ast.Children[0].Type == Expr ? Parser.Group(combs[0]) : combs[0]);
                    }

                    case Primary:
                    {
                        return Result<Combinator>.Ok(combs[0]);
                    }

                    case Suffix:
                    {
                        return Result<Combinator>.Ok(Parser.Token(ast.Lexeme));
                    }

                    case TERMINATOR:
                    {
                        switch (ast.Lexeme[0])
                        {
                            case '"':
                            {
                                var symbol = ast.Lexeme.Substring(1, ast.Lexeme.Length - 2);

                                if (Common.EscapeMap.TryGetValue(symbol, out var value))
                                {
                                    return Result<Combinator>.Ok(Parser.Token(value));
                                }

                                return Result<Combinator>.Ok(tokenRegex.IsMatch(symbol)
                                    ? Parser.Token(symbol, useDefaultSepCheck: true)
                                    : Parser.Token(symbol));
                            }

                            case '#':
                            {
                                var symbol = "#";
                                for (var i = 1; i < ast.Lexeme.Length; i++)
                                {
                                    if (ast.Lexeme[i] == '#')
                                    {
                                        symbol += '#';
                                        break;
                                    }

                                    symbol += ast.Lexeme[i];
                                }

                                switch (symbol)
                                {
                                    case "#re#":
                                    {
                                        var reSymbol = ast.Lexeme.Substring(symbol.Length,
                                            ast.Lexeme.Length - 2 * symbol.Length);
                                        return Result<Combinator>.Ok(Parser.ReToken(reSymbol));
                                    }
                                    case "#native#":
                                    {
                                        var funcSrc = ast.Lexeme.Substring(symbol.Length,
                                            ast.Lexeme.Length - 2 * symbol.Length);
                                        return Result<Combinator>.Ok(Parser.NativeHandleToken("_", funcSrc));
                                    }
                                }

                                break;
                            }
                        }

                        return Result<Combinator>.Err(new GenError($"invalid TERMINATOR {ast.Lexeme}"));
                    }

                    case SYMBOL:
                    {
                        return Result<Combinator>.Ok(grammar[ast.Lexeme]);
                    }

                    case EOL:
                    {
                        return Result<Combinator>.Ok(grammar[EOL]);
                    }

                    case EOF:
                    {
                        return Result<Combinator>.Ok(grammar[EOF]);
                    }

                    default:
                        return Result<Combinator>.Err(new GenError($"invalid type {ast.Type}"));
                }
            }

            var ret = Parser.VisitAstPostOrder<Combinator>(rootAst, Handle);

            if (!ret.IsSuccess)
            {
                return Result<string>.Err(ret.Error);
            }

            var grammarCodeSrc = grammar.GrammarCodeStr(productList.ToArray());

            var finalStr = Template.Replace("${ClassName}", className)
                .Replace("${ProductList}", string.Join(", ", productList.Select(i => $"\"{i}\"")))
                .Replace("${GrammarBuild}", grammarCodeSrc);

            return Result<string>.Ok(finalStr);
        }
    }
}