using GestStack.API.Authorization;
using GestStack.API.Contracts.Roles;
using GestStack.Application.Common.Interfaces;
using GestStack.Application.Common.Security;
using Microsoft.AspNetCore.Mvc;

namespace GestStack.API.Controllers;

[ApiController]
[Route("users")]
public class UsersController(IRoleService roleService) : ControllerBase
{
    [HttpPost("{username}/roles")]
    [HasPermission(Permissions.Users.Roles.Assign)]
    public async Task<IActionResult> AssignRole(string username, AssignRoleRequest info)
    {
        var result = await roleService.AssignToUserAsync(username, info.Role);

        if (!result.Succeeded)
            return BadRequest(new { result.Errors });

        return NoContent();
    }

    [HttpDelete("{username}/roles/{role}")]
    [HasPermission(Permissions.Users.Roles.Assign)]
    public async Task<IActionResult> RemoveRole(string username, string role)
    {
        var result = await roleService.RemoveFromUserAsync(username, role);

        if (!result.Succeeded)
            return BadRequest(new { result.Errors });

        return NoContent();
    }
}
