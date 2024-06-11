using System.Collections.Generic;

namespace CSCombinator
{
    public struct ParseRet
    {
        public readonly AstNode AstNode;
        public readonly uint Offset;

        public ParseRet(AstNode node, uint offset)
        {
            AstNode = node;
            Offset = offset;
        }
    }

    public struct ParseCallbackRet
    {
        public int Pos;
        public string Lexeme;
        public List<AstNode> Children;
        public uint Offset;
    }

    public delegate Result<ParseCallbackRet> ParseCallback(string src, uint offset);

    public class Combinator
    {
        public string Name { get; set; }
        public string Info { get; set; }
        public string Code { get; set; }
        public ParseCallback ParseCb { get; set; }

        internal Combinator Parent { get; set; }

        public Combinator(string name, string info, string code, ParseCallback parseCb)
        {
            Name = name;
            Info = info;
            Code = code;
            ParseCb = parseCb;
        }

        public Result<ParseRet> Parse(string src, uint offset)
        {
            if (ParseCb == null)
            {
                return Result<ParseRet>.Err(new NoParseCallbackError(this));
            }

            var ret = ParseCb.Invoke(src, offset);

            // Console.WriteLine(
            //     $"invoke {Name}, {Info}, ret: {ret.Ret},offset: {offset}, retOffset {ret.Ret.Offset}, at {src.SafeSubstring((int)(ret.IsSuccess ? ret.Ret.Offset : offset), 30)}");

            if (!ret.IsSuccess)
            {
                return Result<ParseRet>.Err(ret.Error);
            }

            if (!Name.IsInnerSymbol())
            {
                var node = new AstNode(Name,
                    ret.Ret.Lexeme,
                    ret.Ret.Pos,
                    Common.ReplaceInnerAstNode(ret.Ret.Children));
                return Result<ParseRet>.Ok(new ParseRet(node, ret.Ret.Offset));
            }

            if (!ret.Ret.Children.IsNullOrEmpty())
            {
                return Result<ParseRet>.Ok(new ParseRet(
                    new AstNode(Name,
                        ret.Ret.Lexeme,
                        ret.Ret.Pos,
                        ret.Ret.Children),
                    ret.Ret.Offset));
            }

            return Result<ParseRet>.Ok(new ParseRet(
                new AstNode(Name,
                    ret.Ret.Lexeme,
                    ret.Ret.Pos,
                    null),
                ret.Ret.Offset));
        }

        private Combinator Add(Combinator other)
        {
            var name = $"{Common.InnerSymbol}({Name} + {other.Name})";
            var info =
                $"{(Name.IsInnerSymbol() ? Info : Name)} {(other.Name.IsInnerSymbol() ? other.Info : other.Name)}";
            var code = $"({Code} + {other.Code})";

            return new Combinator(
                name,
                info,
                code,
                (src, offset) =>
                {
                    var pos = (int) offset;

                    var ret = Parse(src, offset);
                    if (!ret.IsSuccess)
                    {
                        return Result<ParseCallbackRet>.Err(ret.Error);
                    }

                    var ret2 = other.Parse(src, ret.Ret.Offset);
                    if (!ret2.IsSuccess)
                    {
                        return Result<ParseCallbackRet>.Err(ret2.Error);
                    }

                    var children = new List<AstNode>();
                    var lexemes = new List<string>();

                    if (ret.Ret.AstNode != null)
                    {
                        lexemes.Add(ret.Ret.AstNode.Lexeme);
                        children.Add(ret.Ret.AstNode);
                    }

                    if (ret2.Ret.AstNode != null)
                    {
                        lexemes.Add(ret2.Ret.AstNode.Lexeme);
                        children.Add(ret2.Ret.AstNode);
                    }

                    return Result<ParseCallbackRet>.Ok(new ParseCallbackRet
                    {
                        Lexeme = Common.LexemeConcat(lexemes),
                        Pos = pos,
                        Children = children,
                        Offset = ret2.Ret.Offset
                    });
                });
        }

        private Combinator Bor(Combinator other)
        {
            var name = $"{Common.InnerSymbol}({Name} | {other.Name})";
            var info =
                $"{(Name.IsInnerSymbol() ? Info : Name)} | {(other.Name.IsInnerSymbol() ? other.Info : other.Name)}";
            var code = $"({Code} | {other.Code})";

            return new Combinator(
                name,
                info,
                code,
                (src, offset) =>
                {
                    var pos = (int)offset;
                    
                    var ret = Parse(src, offset);
                    if (ret.IsSuccess)
                    {
                        return Result<ParseCallbackRet>.Ok(new ParseCallbackRet
                        {
                            Lexeme = ret.Ret.AstNode == null ? "" : ret.Ret.AstNode.Lexeme,
                            Pos = pos,
                            Children = ret.Ret.AstNode == null ? null : new List<AstNode> {ret.Ret.AstNode},
                            Offset = ret.Ret.Offset
                        });
                    }

                    ret = other.Parse(src, offset);
                    if (ret.IsSuccess)
                    {
                        return Result<ParseCallbackRet>.Ok(new ParseCallbackRet
                        {
                            Lexeme = ret.Ret.AstNode == null ? "" : ret.Ret.AstNode.Lexeme,
                            Pos = pos,
                            Children = ret.Ret.AstNode == null ? null : new List<AstNode> {ret.Ret.AstNode},
                            Offset = ret.Ret.Offset
                        });
                    }

                    return Result<ParseCallbackRet>.Err(ret.Error);
                });
        }

        public override string ToString()
        {
            return $"{Name}: {Info}";
        }

        public static Combinator operator +(Combinator a, Combinator b)
        {
            return a.Add(b);
        }

        public static Combinator operator |(Combinator a, Combinator b)
        {
            return a.Bor(b);
        }
    }
}