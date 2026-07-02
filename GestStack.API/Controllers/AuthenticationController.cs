using GestStack.API.Contracts.Auth;
using GestStack.Application.Common.Interfaces;
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

        return Ok(new AuthResponse(result.Token!));
    }

    [HttpPost("[action]")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest info)
    {
        var result = await authService.LoginAsync(info.Username, info.Password);

        if (!result.Succeeded)
            return Unauthorized(new { result.Errors });

        return Ok(new AuthResponse(result.Token!));
    }
}
