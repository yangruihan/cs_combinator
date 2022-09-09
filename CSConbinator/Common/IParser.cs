namespace CSConbinator
{
    public interface IParser
    {
        Grammar Grammar { get; }
        
        Result<AstNode> Parse(string src);

        string GrammarString();

        string GrammarCodeString();
    }
}