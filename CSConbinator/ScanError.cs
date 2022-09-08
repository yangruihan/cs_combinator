namespace CSConbinator
{
    public abstract class ScanError : IError
    {
        public abstract string Error();
    }
    
    public class NoMatchRuleError : ScanError
    {
        private readonly string _src;

        public NoMatchRuleError(string src)
        {
            _src = src;
        }

        public override string Error()
        {
            return $"NoMatchRuleError: at {_src}";
        }
    }
}