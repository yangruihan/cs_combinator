using static CSCombinator.Parser;

namespace CSCombinator
{
    public class GrammarParser : IParser
    {
        public const string Products = "products";
        public const string Product = "product";
        public const string Expr = "expr";
        public const string OrExpr = "or_expr";
        public const string Many = "many";
        public const string Paren = "paren";
        public const string Primary = "primary";
        public const string Suffix = "suffix";
        public const string TERMINATOR = "TERMINATOR";
        public const string SYMBOL = "SYMBOL";
        public const string EOL = "EOL";
        public const string EOF = "EOF";

        private readonly Grammar _g = new Grammar();

        private readonly string[] _productList =
        {
            Products, Product, Expr, OrExpr, Many, Paren, Primary, Suffix, TERMINATOR, SYMBOL, EOL
        };

        public GrammarParser()
        {
            _g[Products] = Many(_g[Product] + Many1(Token(";")) + Many(_g[EOL])) + _g[EOF];
            _g[Product] = _g[SYMBOL] + Token("=") + _g[Expr];
            _g[Expr] = Many1(_g[OrExpr] + Many(_g[EOL]));
            _g[OrExpr] = _g[Many] + Many(Many(_g[EOL]) + Token("|") + _g[Many]);
            _g[Many] = _g[Paren] + Maybe(_g[Suffix]);
            _g[Paren] = _g[Primary] | Group(Token("(") + _g[Expr] + Token(")"));
            _g[Primary] = _g[TERMINATOR] | _g[SYMBOL];
            _g[Suffix] = Token("*") | Token("?") | Token("+");
            _g[TERMINATOR] = UserToken(TERMINATOR, (src, offset) =>
            {
                switch (src[(int)offset])
                {
                    case '"':
                    {
                        var len = 1;
                        var i = (int)(offset + 1);
                        while (i < src.Length)
                        {
                            if (src[i] == '\\' && src[i + 1] == '"')
                            {
                                i++;
                                len++;
                            }
                            else if (src[i] == '"')
                            {
                                len++;
                                break;
                            }

                            len++;
                            i++;
                        }

                        return (uint)len;
                    }
                    case '#':
                    {
                        var symbol = "#";

                        for (var i = (int)(offset + 1); i < src.Length; i++)
                        {
                            if (src[i] != '#')
                            {
                                symbol += src[i];
                            }
                            else
                            {
                                symbol += "#";
                                break;
                            }
                        }

                        var len = symbol.Length;
                        var idx = offset + len;
                        while (idx < src.Length)
                        {
                            if (src.SafeSubstring((int)idx, symbol.Length) == symbol)
                            {
                                len += symbol.Length;
                                break;
                            }

                            len++;
                            idx++;
                        }

                        return (uint)len;
                    }
                    default:
                        return 0;
                }
            });
            _g[SYMBOL] = ReToken(@"[A-Za-z_][\w]*");
            _g[EOL] = Token("\n");
            _g[EOF] = Eof();
        }

        public Grammar Grammar => _g;

        public Result<AstNode> Parse(string grammar)
        {
            return Parser.Parse(grammar, _g[_productList[0]]);
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
            return $"Grammar:\n{_g.GrammarStr(_productList)}";
        }
    }
}