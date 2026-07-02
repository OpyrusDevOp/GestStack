using GestStack.API.Contracts.Auth;
using Microsoft.AspNetCore.Mvc;

namespace GestStack.API.Controllers;

[ApiController]
[Route("auth")]
public class AuthenticationController : ControllerBase
{
    [HttpPost("[action]")]
    public async Task<ActionResult> Register(RegisterRequest info)
    {
        return Ok("");
    }

    [HttpPost("[action]")]
    public async Task<ActionResult> Login(LoginRequest info)
    {
        return Ok("");
    }
}
