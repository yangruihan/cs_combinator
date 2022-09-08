namespace CSConbinator
{
    public interface IError
    {
        string Error();
    }

    public abstract class SimpleError : ParseError
    {
        private readonly string _message;

        protected abstract string ErrorName { get; }

        protected SimpleError(string message)
        {
            _message = message;
        }

        public override string Error()
        {
            return $"{ErrorName}: {_message}";
        }
    }
}