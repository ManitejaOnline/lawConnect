namespace LawConnect.Application.Common.Models;

/// <summary>
/// Represents the outcome of an operation without a return value.
/// </summary>
public class Result
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> class.
    /// </summary>
    /// <param name="isSuccess">Whether the operation succeeded.</param>
    /// <param name="error">Error details when the operation failed.</param>
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("Successful result cannot contain an error.");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("Failed result must contain an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error associated with a failed result.
    /// </summary>
    public Error Error { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful <see cref="Result"/>.</returns>
    public static Result Success() => new(true, Error.None);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <returns>A failed <see cref="Result"/>.</returns>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>
    /// Creates a failed result from a code and message.
    /// </summary>
    /// <param name="code">Error code.</param>
    /// <param name="message">Error message.</param>
    /// <returns>A failed <see cref="Result"/>.</returns>
    public static Result Failure(string code, string message) => new(false, Error.Failure(code, message));

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>A successful <see cref="Result{T}"/>.</returns>
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    /// <summary>
    /// Creates a failed result with a value type.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="error">The error.</param>
    /// <returns>A failed <see cref="Result{T}"/>.</returns>
    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);

    /// <summary>
    /// Creates a failed result with a value type from a code and message.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="code">Error code.</param>
    /// <param name="message">Error message.</param>
    /// <returns>A failed <see cref="Result{T}"/>.</returns>
    public static Result<T> Failure<T>(string code, string message) => Result<T>.Failure(Error.Failure(code, message));
}

/// <summary>
/// Represents the outcome of an operation that returns a value.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
public class Result<T> : Result
{
    private readonly T? _value;

    private Result(bool isSuccess, T? value, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// Gets the value when the result is successful.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the result is a failure.</exception>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on a failed result.");

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>A successful <see cref="Result{T}"/>.</returns>
    public static Result<T> Success(T value) => new(true, value, Error.None);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <returns>A failed <see cref="Result{T}"/>.</returns>
    public static new Result<T> Failure(Error error) => new(false, default, error);

    /// <summary>
    /// Creates a failed result from a code and message.
    /// </summary>
    /// <param name="code">Error code.</param>
    /// <param name="message">Error message.</param>
    /// <returns>A failed <see cref="Result{T}"/>.</returns>
    public static new Result<T> Failure(string code, string message) =>
        new(false, default, Error.Failure(code, message));
}
