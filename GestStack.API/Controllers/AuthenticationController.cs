using System.Security.Claims;
using GestStack.API.Contracts.Auth;
using GestStack.Application.Common.Interfaces;
using GestStack.Application.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestStack.API.Controllers;

[ApiController]
[Route("auth")]
public class AuthenticationController(IAuthService authService) : ControllerBase
{
    [HttpPost("[action]")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest info)
    {
        var result = await authService.RegisterAsync(info.Username, info.Fullname, info.Password);

        if (!result.Succeeded)
            return BadRequest(new { result.Errors });

        return Ok(new AuthResponse(result.Token!, result.RefreshToken!));
    }

    [HttpPost("[action]")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest info)
    {
        var result = await authService.LoginAsync(info.Username, info.Password);

        if (!result.Succeeded)
            return Unauthorized(new { result.Errors });

        return Ok(new AuthResponse(result.Token!, result.RefreshToken!));
    }

    [HttpPost("[action]")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest info)
    {
        var result = await authService.RefreshAsync(info.RefreshToken);

        if (!result.Succeeded)
            return Unauthorized(new { result.Errors });

        return Ok(new AuthResponse(result.Token!, result.RefreshToken!));
    }

    [HttpPost("[action]")]
    public async Task<IActionResult> Logout(RefreshRequest info)
    {
        await authService.RevokeAsync(info.RefreshToken);
        return NoContent();
    }

    [HttpGet("[action]")]
    [Authorize]
    public IActionResult Me() =>
        Ok(
            new
            {
                Username = User.Identity?.Name,
                Roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value),
                Permissions = User.FindAll(CustomClaims.Permission).Select(c => c.Value),
            }
        );
}
