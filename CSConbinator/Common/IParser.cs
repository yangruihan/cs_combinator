using System.IO;

namespace CSConbinator
{
    public interface IParser
    {
        Grammar Grammar { get; }

        Result<AstNode> Parse(string src);

        string GrammarString();

        string GrammarCodeString();
    }

    public static class ParserUtils
    {
        public static Result<AstNode> ParseFile(this IParser parser, string filePath)
        {
            return parser.Parse(File.ReadAllText(filePath).Trim());
        }
    }
}