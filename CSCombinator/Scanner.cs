using System.Collections.Generic;

namespace CSCombinator
{
    public class Scanner
    {
        public Result<Token[]> Scan(string src, TokenRule[] rules)
        {
            var ret = new List<Token>();
            int offset = 0;

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
                    return Result<Token[]>.Err(new NoMatchRuleError(src.SafeSubstring((int)offset, 10)));
                }
            }

            return Result<Token[]>.Ok(ret.ToArray());
        }
    }
}