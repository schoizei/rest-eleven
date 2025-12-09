namespace RestEleven.Shared.Results;

public class SimpleResult<T>
{
    private readonly List<string> _errors = new();

    public bool Succeeded { get; private init; }
    public T? Value { get; private init; }
    public IReadOnlyCollection<string> Errors => _errors;

    public static SimpleResult<T> Success(T value) => new()
    {
        Succeeded = true,
        Value = value
    };

    public static SimpleResult<T> Failure(params string[] errors)
    {
        var instance = new SimpleResult<T> { Succeeded = false };
        if (errors.Length > 0)
        {
            instance._errors.AddRange(errors);
        }

        return instance;
    }
}
