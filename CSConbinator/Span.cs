using System;

namespace CSConbinator
{
    [Serializable]
    public struct Span
    {
        public uint Start;
        public uint Len;

        public Span(uint start, uint len)
        {
            Start = start;
            Len = len;
        }

        public string Str(string str)
        {
            return str.Substring((int) Start, (int) Len);
        }

        public override string ToString()
        {
            return $"(Span start: {Start}, len: {Len})";
        }
    }
}