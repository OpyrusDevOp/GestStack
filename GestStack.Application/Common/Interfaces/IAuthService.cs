using GestStack.Application.Common.Models;

namespace GestStack.Application.Common.Interfaces;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string username, string fullName, string password);
    Task<AuthResult> LoginAsync(string username, string password);
    Task<AuthResult> RefreshAsync(string refreshToken);
    Task RevokeAsync(string refreshToken);
}
