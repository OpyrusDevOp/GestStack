using GestStack.API.Authorization;
using GestStack.API.Contracts.Setup;
using GestStack.Application.Common.Interfaces;
using GestStack.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
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
    [Authorize(Policy = SetupAuth.Policy)]
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

    [HttpPost("company-profile")]
    [Authorize(Policy = SetupAuth.Policy)]
    public async Task<IActionResult> CreateCompanyProfile(CreateCompanyProfileRequest request)
    {
        var result = await setupService.CreateCompanyProfileAsync(
            request.LegalName,
            request.Email,
            request.Country,
            request.Logo,
            request.LogoContentType
        );
        if (!result.Succeeded)
            return result.ErrorCode == SetupErrorCodes.CompanyProfileAlreadyExists
                ? Conflict(new { result.Errors })
                : BadRequest(new { result.Errors });

        return Created();
    }
}
