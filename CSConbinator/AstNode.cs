using System.Collections.Generic;

namespace CSConbinator
{
    public class AstNode
    {
        public string Type { get; }
        public string Lexeme { get; }
        public List<AstNode> Children { get; }

        public AstNode(string type, string lexeme, List<AstNode> children)
        {
            Type = type;
            Lexeme = lexeme;
            Children = children;
        }

        public override string ToString()
        {
            string ToReadable(string src)
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

            return $"<AstNode type: {Type}, lexeme: {ToReadable(Lexeme).Truncate(30)}, child_count: {Children.Count}>";
        }
    }
}