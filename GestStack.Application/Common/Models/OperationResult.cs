namespace GestStack.Application.Common.Models;

public record OperationResult
{
    public bool Succeeded { get; private init; }
    public IReadOnlyList<string> Errors { get; private init; } = [];

    public static OperationResult Success() => new() { Succeeded = true };

    public static OperationResult Failure(params IEnumerable<string> errors) =>
        new() { Errors = [.. errors] };
}
