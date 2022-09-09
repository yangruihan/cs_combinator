using System;
using System.IO;
using CSConbinator;

namespace GrammarParseTest
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var parser = new GrammarParser();
            Console.Write(parser);
            var ret = parser.Parse(File.ReadAllText("grammar.txt").Trim());
            if (ret.IsSuccess)
            {
                Console.Write(Parser.AstStr(ret.Ret));
            }
            else
            {
                Console.Error.Write(ret.Error.Error());
            }
        }
    }
}