namespace GestStack.Application.Common.Models;

public record AuthResult
{
    public bool Succeeded { get; private init; }
    public string? Token { get; private init; }
    public IReadOnlyList<string> Errors { get; private init; } = [];

    public static AuthResult Success(string token) =>
        new() { Succeeded = true, Token = token };

    public static AuthResult Failure(params IEnumerable<string> errors) =>
        new() { Errors = [.. errors] };
}
