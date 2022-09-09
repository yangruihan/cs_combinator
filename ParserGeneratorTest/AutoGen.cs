
// ---------------
// Auto Generated
// ---------------

using CSConbinator;

using static CSConbinator.Parser;

public class GrammarParser: IParser
{
    private readonly Grammar _g = new Grammar();

    public Grammar Grammar => _g;

    private readonly string[] _productList =
    {
        "products", "product", "expr", "or_expr", "many", "paren", "primary", "SUFFIX", "TERMINATOR", "SYMBOL", "EOL"
    };

    public GrammarParser()
    {
        _g["products"] = (Many(Group(((_g["product"] + Many1(Group(Token(";")))) + Many(Group(_g["EOL"]))))) + _g["EOF"]);

_g["product"] = ((_g["SYMBOL"] + Token("=")) + _g["expr"]);

_g["expr"] = Many1(Group((_g["or_expr"] + Many(Group(_g["EOL"])))));

_g["or_expr"] = (_g["many"] + Many(Group(((Many(Group(_g["EOL"])) + Token("|")) + _g["many"]))));

_g["many"] = (_g["paren"] + Maybe(Group(_g["SUFFIX"])));

_g["paren"] = (_g["primary"] | Group(((Token("(") + Many1(Group((_g["or_expr"] + Many(Group(_g["EOL"])))))) + Token(")"))));

_g["primary"] = (_g["TERMINATOR"] | _g["SYMBOL"]);

_g["SUFFIX"] = ((Token("*") | Token("?")) | Token("+"));

_g["TERMINATOR"] = UserToken("_", 

            (src, offset) =>
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
            }
            );

_g["SYMBOL"] = ReToken(@"[A-Za-z_][\w]*");

_g["EOL"] = Token("\n");

_g["EOF"] = Eof();

    }

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