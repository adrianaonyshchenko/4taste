namespace FourTaste.ApplicationCore.Results
{
    public class Result<T> : Result
    {
        private Result(bool isSuccess, T? value, string? error)
            : base(isSuccess, error)
        {
            this.Value = value;
        }

        public T? Value { get; }

        public static Result<T> Success(T value)
        {
            return new Result<T>(true, value, null);
        }

        public static new Result<T> Failure(string error)
        {
            return new Result<T>(false, default, error);
        }
    }
}
