using Project.Shared.Types;

namespace Project.Shared.DTOs;

public class Result
{
    public ResultCode Code { get; }
    public string Message { get; }
    public bool IsSuccess => Code == ResultCode.Success;

    protected Result(ResultCode code, string message)
    {
        Code = code;
        Message = message;
    }

    public static Result Success() => new(ResultCode.Success, string.Empty);
    public static Result Failure(ResultCode code, string message) => new(code, message);
}

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(ResultCode code, string message, T? value) : base(code, message)
    {
        Value = value;
    }

    public static Result<T> Success(T? value) => new(ResultCode.Success, string.Empty, value);
    public static new Result<T> Failure(ResultCode code, string message) => new(code, message, default);
    public static Result<T> Failure(Result result) => new(result.Code, result.Message, default);
}
