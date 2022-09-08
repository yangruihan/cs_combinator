using System.Collections.Generic;

namespace CSConbinator
{
    public class Scanner
    {
        public Result<List<Token>> Scan(string src, TokenRule[] rules)
        {
            var ret = new List<Token>();
            uint offset = 0;

            while (offset < src.Length)
            {
                var parseSuc = false;

                foreach (var rule in rules)
                {
                    if (!rule.TryParse(src, offset, out var token)) continue;

                    offset += token.Span.Len;
                    ret.Add(token);
                    parseSuc = true;

                    break;
                }

                if (!parseSuc)
                {
                    return Result<List<Token>>.Err(new NoMatchRuleError(src.Substring((int) offset, 10)));
                }
            }

            return Result<List<Token>>.Ok(ret);
        }
    }
}