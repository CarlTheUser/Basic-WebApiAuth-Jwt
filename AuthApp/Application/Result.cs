namespace Application
{
    public record class Result
    {
        public static Result Ok() => new(success: true, message: string.Empty);

        public static Result<T> Ok<T>(T value) => new(value: value, success: true, message: string.Empty);

        public static Result OkWithMessage(string message) => new(success: true, message: message);

        public static Result<T> OkWithMessage<T>(T value, string message) => new(value: value, success: true, message: message);

        public static Result Fail(string message) => new(success: false, message: message);

        public static Result<T> Fail<T>(string message) => new(value: default, success: false, message: message);

        public static Result<T> Fail<T>(T value, string message) => new(value: value, success: false, message: message);

        public bool Success { get; }

        public string Message { get; } = string.Empty;

        public Result(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }

    public record class Result<T> : Result
    {
        public Result(T value, bool success, string message) : base(success, message)
        {
            Value = value;
        }

        public T Value { get; }


    }
}
