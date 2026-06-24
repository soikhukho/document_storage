namespace DocumentStorage.Shared.Results;

/// <summary>
/// Result of an operation with no return value.
/// </summary>
public class Result
{
    public bool IsSuccess { get; protected set; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<AppError> Errors { get; protected set; }

    protected Result(bool isSuccess, IReadOnlyList<AppError> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors ?? new List<AppError>();
    }

    public static Result Success()
        => new(true, new List<AppError>());

    public static Result Failure(AppError error)
        => Failure(new[] { error });

    public static Result Failure(params AppError[] errors)
        => Failure(errors.AsEnumerable());

    public static Result Failure(IEnumerable<AppError> errors)
    {
        var errorList = errors?.ToList() ?? new List<AppError>();
        if (errorList.Count == 0)
            throw new ArgumentException("At least one error must be provided.", nameof(errors));

        return new Result(false, errorList.AsReadOnly());
    }

    public AppError? FirstError => IsFailure ? Errors.FirstOrDefault() : null;
}

/// <summary>
/// Result of an operation that returns a value of type T.
/// </summary>
public class Result<T> : Result
{
    public T? Value { get; private set; }

    private Result(bool isSuccess, T? value, IReadOnlyList<AppError> errors)
        : base(isSuccess, errors)
    {
        Value = value;
    }

    public static Result<T> Success(T value)
        => new(true, value, new List<AppError>());

    public static new Result<T> Failure(AppError error)
        => Failure(new[] { error });

    public static new Result<T> Failure(params AppError[] errors)
        => Failure(errors.AsEnumerable());

    public static new Result<T> Failure(IEnumerable<AppError> errors)
    {
        var errorList = errors?.ToList() ?? new List<AppError>();
        if (errorList.Count == 0)
            throw new ArgumentException("At least one error must be provided.", nameof(errors));

        return new Result<T>(false, default, errorList.AsReadOnly());
    }
}
