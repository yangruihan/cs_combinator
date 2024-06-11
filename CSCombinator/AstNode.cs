using System.Collections.Generic;
using System.Linq;

namespace CSCombinator
{
    public class AstNode
    {
        public string Type { get; }
        public string Lexeme { get; }
        public List<AstNode> Children { get; }
        public Span Span { get; }

        public AstNode(string type, string lexeme, int pos, List<AstNode> children)
        {
            Type = type;
            Lexeme = lexeme;
            Children = children;

            Span = new Span(pos, lexeme.Length);
            if (children != null)
            {
                Span += children.Last().Span;
            }
        }

        private static string ToReadable(string src)
        {
            switch (src)
            {
                case "\n":
                {
                    return "\\n";
                }
                case "\t":
                {
                    return "\\t";
                }
                case "\r":
                {
                    return "\\r";
                }
            }

            return src;
        }

        public override string ToString()
        {
            return
                $"< AstNode type: {Type} lexeme:`{ToReadable(Lexeme).Truncate(30)}` child_count: {Children?.Count ?? 0} span: {Span} >";
        }

        public string ToStringWithPos(string filename, string src)
        {
            var posSrc = Span.ToPos(filename, src).ToString();
            return
                $"< AstNode type: {Type} lexeme:`{ToReadable(Lexeme).Truncate(30)}` child_count: {Children?.Count ?? 0} pos: {posSrc} >";
        }
    }
}