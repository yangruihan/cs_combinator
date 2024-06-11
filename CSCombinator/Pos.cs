using System;
using System.Text;

namespace CSCombinator
{
    [Serializable]
    public class Pos
    {
        public string Filename { get; }
        public int Offset { get; }
        public int Line { get; }
        public int Column { get; }

        public Pos(string filename, string src, int offset)
        {
            Filename = filename;
            Offset = offset;

            if (offset < 0)
            {
                return;
            }

            Line = 1;
            Column = 1;

            for (var i = 0; i < offset; i++)
            {
                Column++;
                if (src[i] == '\n')
                {
                    Line++;
                    Column = 1;
                }
            }
        }

        public bool IsValid()
        {
            return Line > 0;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Filename);
            if (IsValid())
            {
                if (sb.Length > 0)
                    sb.Append(":");

                sb.Append(Line);
                if (Column != 0)
                    sb.AppendFormat(":{0}", Column);
            }

            if (sb.Length == 0)
                sb.Append("-");

            return sb.ToString();
        }
    }
}