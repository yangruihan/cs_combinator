using System.Diagnostics;
using System.Text;

namespace CSConbinator
{
    public interface IError
    {
        string Error();
    }

    public abstract class SimpleError : ParseError
    {
        private readonly string _message;
        private readonly string _frameInfo;

        protected abstract string ErrorName { get; }

        protected SimpleError(string message)
        {
            _message = message;
            _frameInfo = GetFrameInfo();
        }

        public override string Error()
        {
            // return $"{ErrorName}: {_message}\nTrace:\n{_frameInfo}";
            return $"{ErrorName}: {_message}";
        }

        private string GetFrameInfo()
        {
            // TODO opt
            
            var sb = new StringBuilder();
            // var stack = new StackTrace(3, true);
            // for (var i = 0; i < stack.FrameCount; i++)
            // {
            //     var frame = stack.GetFrame(i);
            //     sb.AppendLine(
            //         $"\tFile: {frame.GetFileName()}  Func: {frame.GetMethod().Name}  Line: {frame.GetFileLineNumber()}");
            // }

            return sb.ToString();
        }
    }
}