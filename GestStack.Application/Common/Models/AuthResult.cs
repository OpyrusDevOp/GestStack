namespace GestStack.Application.Common.Models;

public record AuthResult
{
    public bool Succeeded { get; private init; }
    public string? Token { get; private init; }
    public string? RefreshToken { get; private init; }
    public IReadOnlyList<string> Errors { get; private init; } = [];

    public static AuthResult Success(string token, string refreshToken) =>
        new() { Succeeded = true, Token = token, RefreshToken = refreshToken };

    public static AuthResult Failure(params IEnumerable<string> errors) =>
        new() { Errors = [.. errors] };
}
