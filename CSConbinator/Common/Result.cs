namespace CSConbinator
{
    public struct Result<TRet>
    {
        public TRet Ret;
        public readonly IError Error;

        public bool IsSuccess => Error == null;

        public Result(TRet ret, IError error)
        {
            Ret = ret;
            Error = error;
        }

        public static Result<TRet> Ok(TRet ret)
        {
            return new Result<TRet>(ret, null);
        }

        public static Result<TRet> Err(IError error)
        {
            return new Result<TRet>(default, error);
        }
    }
}