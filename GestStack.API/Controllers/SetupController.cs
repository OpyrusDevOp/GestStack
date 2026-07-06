using GestStack.Application.Common.Interfaces;
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
}
