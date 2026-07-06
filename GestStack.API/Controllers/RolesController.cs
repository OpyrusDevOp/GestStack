using GestStack.API.Authorization;
using GestStack.API.Contracts.Roles;
using GestStack.Application.Common.Interfaces;
using GestStack.Application.Common.Models;
using GestStack.Application.Common.Security;
using Microsoft.AspNetCore.Mvc;

namespace GestStack.API.Controllers;

[ApiController]
[Route("roles")]
public class RolesController(IRoleService roleService) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Roles.Get)]
    public async Task<ActionResult<IReadOnlyList<RoleInfo>>> GetRoles() =>
        Ok(await roleService.GetRolesAsync());

    [HttpGet("/permissions")]
    [HasPermission(Permissions.Roles.Get)]
    public ActionResult<IReadOnlyList<string>> GetPermissions() => Ok(Permissions.All);

    [HttpPost]
    [HasPermission(Permissions.Roles.Create)]
    public async Task<IActionResult> Create(CreateRoleRequest info)
    {
        var result = await roleService.CreateRoleAsync(info.Name);

        if (!result.Succeeded)
            return BadRequest(new { result.Errors });

        return Created($"/roles/{info.Name}", null);
    }

    [HttpDelete("{name}")]
    [HasPermission(Permissions.Roles.Delete)]
    public async Task<IActionResult> Delete(string name)
    {
        var result = await roleService.DeleteRoleAsync(name);

        if (!result.Succeeded)
            return BadRequest(new { result.Errors });

        return NoContent();
    }

    [HttpPut("{name}/permissions")]
    [HasPermission(Permissions.Roles.Modify)]
    public async Task<IActionResult> SetPermissions(string name, SetPermissionsRequest info)
    {
        var result = await roleService.SetPermissionsAsync(name, [.. info.Permissions]);

        if (!result.Succeeded)
            return BadRequest(new { result.Errors });

        return NoContent();
    }
}
