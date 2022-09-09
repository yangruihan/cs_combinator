using System;
using System.IO;
using CSConbinator;

namespace ParserGeneratorTest
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            if (args[0] == "Gen")
            {
                var ret = ParserGenerator.Gen(File.ReadAllText("grammar.txt").Trim(), "GrammarParser");
                if (!ret.IsSuccess)
                {
                    Console.Error.WriteLine(ret.Error.Error());
                }
                else
                {
                    Console.WriteLine(ret.Ret);

                    File.WriteAllText("AutoGen.cs", ret.Ret);
                }
            }
            else
            {
                var parser = new GrammarParser();

                Console.Write(parser);

                Console.WriteLine("------");

                Console.WriteLine(parser.GrammarCodeString());

                Console.WriteLine("------");

                var ret = parser.ParseFile("grammar.txt");
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
}