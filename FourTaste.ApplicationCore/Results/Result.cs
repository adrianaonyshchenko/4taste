namespace FourTaste.ApplicationCore.Results
{
    public class Result
    {
        protected Result(bool isSuccess, string? error)
        {
            this.IsSuccess = isSuccess;
            this.Error = error;
        }

        public bool IsSuccess { get; }

        public bool IsFailure => !this.IsSuccess;

        public string? Error { get; }

        public static Result Success()
        {
            return new Result(true, null);
        }

        public static Result Failure(string error)
        {
            return new Result(false, error);
        }
    }

}
