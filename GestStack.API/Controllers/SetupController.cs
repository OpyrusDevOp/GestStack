using GestStack.API.Contracts.Setup;
using GestStack.Application.Common.Interfaces;
using GestStack.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace GestStack.API.Controllers;

[ApiController]
[Route("setup")]
public class SetupController(ISetupService setupService) : ControllerBase
{
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var result = await setupService.GetStatusAsync();

        return Ok(result);
    }

    [HttpPost("set-admin")]
    public async Task<IActionResult> CreateAdmin(CreateAdminRequest request)
    {
        var result = await setupService.CreateAdminAsync(
            request.Username,
            request.Fullname,
            request.Password
        );
        if (!result.Succeeded)
            return result.ErrorCode == SetupErrorCodes.AdminAlreadyExists
                ? Conflict(new { result.Errors })
                : BadRequest(new { result.Errors });

        return Created();
    }
}
