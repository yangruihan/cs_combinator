namespace CSCombinator
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

    public class ParseUserTokenError : SimpleError
    {
        public ParseUserTokenError(string message) : base(message)
        {
        }

        protected override string ErrorName => nameof(ParseUserTokenError);
    }

    public class ParseNativeHandleTokenError : SimpleError
    {
        public ParseNativeHandleTokenError(string message) : base(message)
        {
        }

        protected override string ErrorName => nameof(ParseNativeHandleTokenError);
    }

    public class ParseReTokenError : SimpleError
    {
        public ParseReTokenError(string message) : base(message)
        {
        }

        protected override string ErrorName => nameof(ParseReTokenError);
    }

    public class ParseTokenSepCheckFuncError : SimpleError
    {
        public ParseTokenSepCheckFuncError(string message) : base(message)
        {
        }

        protected override string ErrorName => nameof(ParseTokenSepCheckFuncError);
    }

    public class ParseTokenError : SimpleError
    {
        public ParseTokenError(string message) : base(message)
        {
        }

        protected override string ErrorName => nameof(ParseTokenError);
    }

    public class ParseMany1Error : SimpleError
    {
        public ParseMany1Error(string message) : base(message)
        {
        }

        protected override string ErrorName => nameof(ParseMany1Error);
    }

    public class ParseEofError : SimpleError
    {
        public ParseEofError(string message) : base(message)
        {
        }

        protected override string ErrorName => nameof(ParseEofError);
    }
}