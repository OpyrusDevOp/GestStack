namespace GestStack.Application.Common.Models;

public abstract record ResultBase<TSelf>
    where TSelf : ResultBase<TSelf>, new()
{
    public bool Succeeded { get; protected init; }
    public string? ErrorCode { get; protected init; }
    public IReadOnlyList<string> Errors { get; protected init; } = [];

    public static TSelf Success() => new() { Succeeded = true };

    public static TSelf Failure(params IEnumerable<string> errors) =>
        new() { Errors = [.. errors] };

    public TSelf WithErrorCode(string errorCode) =>
        (TSelf)(this with { ErrorCode = errorCode });
}
