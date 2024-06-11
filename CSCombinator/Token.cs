using System;

namespace CSCombinator
{
    [Serializable]
    public struct Token
    {
        public string Type;
        public Span Span;

        public Token(string type, Span span)
        {
            Type = type;
            Span = span;
        }

        public string Lexeme(string src)
        {
            return Span.Str(src);
        }

        public override string ToString()
        {
            return $"[token type: {Type}, span: {Span}]";
        }
    }
}