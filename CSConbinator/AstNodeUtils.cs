namespace CSConbinator
{
    public static class AstNodeUtils
    {
        public static bool IsEOL(this AstNode node)
        {
            return node.Type == GrammarParser.EOL;
        }
    }
}