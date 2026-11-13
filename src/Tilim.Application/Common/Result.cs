namespace Tilim.Application.Common;

public enum ErrorKind
{
    Validation,
    Conflict,
    NotFound
}

public sealed record Error(ErrorKind Kind, string Message)
{
    public static Error Validation(string message) => new(ErrorKind.Validation, message);
    public static Error Conflict(string message) => new(ErrorKind.Conflict, message);
    public static Error NotFound(string message) => new(ErrorKind.NotFound, message);
}

/// <summary>
/// A tiny Result type so application services can report expected failures
/// (slug taken, worker not found, ...) without throwing.
/// </summary>
public readonly struct Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error? Error { get; }

    private Result(bool isSuccess, T? value, Error? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(Error error) => new(false, default, error);
}
