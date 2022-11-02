using System.Text.RegularExpressions;

namespace CSConbinator
{
    public class TokenRule
    {
        public string Type { get; }
        public string Rule { get; }

        private readonly Regex _ruleRegex;

        public TokenRule(string type, string rule)
        {
            Type = type;
            Rule = rule;

            _ruleRegex = new Regex(rule, RegexOptions.Compiled);
        }

        public bool TryParse(string src, int offset, out Token token)
        {
            token = default;

            var matchRet = _ruleRegex.Match(src, offset);
            if (matchRet.Success)
            {
                token = new Token(Type, new Span
                {
                    Len = matchRet.Length,
                    Start = offset
                });

                return true;
            }

            return false;
        }
    }
}