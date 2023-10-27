using System;
using System.Diagnostics;

namespace CSConbinator
{
    [Serializable]
    public struct Span
    {
        public int Start;
        public int Len;

        public Span(int start, int len)
        {
            Start = start;
            Len = len;
        }

        public string Str(string str)
        {
            return str.Substring(Start, Len);
        }

        public bool Valid()
        {
            return Len > 0;
        }

        public static Span operator +(Span a, Span b)
        {
            Debug.Assert(a.Start <= b.Start);

            Span ret;
            ret.Start = a.Start;
            ret.Len = b.Start + b.Len - a.Start;

            return ret;
        }

        public override string ToString()
        {
            return $"(Span start: {Start}, len: {Len})";
        }

        public Pos ToPos(string filename, string src)
        {
            return new Pos(filename, src, Start);
        }
    }
}