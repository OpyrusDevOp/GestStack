namespace GestStack.Application.Common.Models;

public record AuthResult : ResultBase<AuthResult>
{
    public string? Token { get; private init; }
    public string? RefreshToken { get; private init; }

    public static AuthResult Success(string token, string refreshToken) =>
        new() { Succeeded = true, Token = token, RefreshToken = refreshToken };
}
