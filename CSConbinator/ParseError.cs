namespace CSConbinator
{
    public abstract class ParseError : IError
    {
        public abstract string Error();
    }

    public class NoParseCallbackError : ParseError
    {
        private readonly Combinator _combinator;

        public NoParseCallbackError(Combinator combinator)
        {
            _combinator = combinator;
        }

        public override string Error()
        {
            return $"NoParseCallbackError: combinator {_combinator.Name} {_combinator.Info}";
        }
    }
}