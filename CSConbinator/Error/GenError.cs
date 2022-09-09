namespace CSConbinator
{
    public class NoProductFoundError : SimpleError
    {
        public NoProductFoundError(string message) : base(message)
        {
        }

        protected override string ErrorName => nameof(NoProductFoundError);
    }

    public class GenError : SimpleError
    {
        public GenError(string message) : base(message)
        {
        }

        protected override string ErrorName => nameof(GenError);
    }
}