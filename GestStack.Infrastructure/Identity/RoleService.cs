using System.Security.Claims;
using GestStack.Application.Common.Interfaces;
using GestStack.Application.Common.Models;
using GestStack.Application.Common.Security;
using GestStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace GestStack.Infrastructure.Identity;

public class RoleService(RoleManager<IdentityRole> roleManager, UserManager<AppUser> userManager)
    : IRoleService
{
    public async Task<IReadOnlyList<RoleInfo>> GetRolesAsync()
    {
        var roles = new List<RoleInfo>();

        foreach (var role in roleManager.Roles.ToList())
        {
            var permissions = (await roleManager.GetClaimsAsync(role))
                .Where(c => c.Type == CustomClaims.Permission)
                .Select(c => c.Value)
                .Order()
                .ToList();
            roles.Add(new RoleInfo(role.Name!, permissions));
        }

        return roles;
    }

    public async Task<OperationResult> CreateRoleAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return OperationResult.Failure("Role name is required.");

        if (await roleManager.FindByNameAsync(name) is not null)
            return OperationResult.Failure($"Role '{name}' already exists.");

        return ToResult(await roleManager.CreateAsync(new IdentityRole(name)));
    }

    public async Task<OperationResult> DeleteRoleAsync(string name)
    {
        if (name == Roles.Administrator)
            return OperationResult.Failure("The Administrator role cannot be deleted.");

        var role = await roleManager.FindByNameAsync(name);
        if (role is null)
            return OperationResult.Failure($"Role '{name}' does not exist.");

        return ToResult(await roleManager.DeleteAsync(role));
    }

    public async Task<OperationResult> SetPermissionsAsync(
        string roleName,
        IReadOnlyCollection<string> permissions
    )
    {
        if (roleName == Roles.Administrator)
            return OperationResult.Failure(
                "The Administrator role always has all permissions and cannot be modified."
            );

        var unknown = permissions.Except(Permissions.All).ToList();
        if (unknown.Count > 0)
            return OperationResult.Failure(unknown.Select(p => $"Unknown permission '{p}'."));

        var role = await roleManager.FindByNameAsync(roleName);
        if (role is null)
            return OperationResult.Failure($"Role '{roleName}' does not exist.");

        var current = (await roleManager.GetClaimsAsync(role))
            .Where(c => c.Type == CustomClaims.Permission)
            .ToList();

        foreach (var claim in current.Where(c => !permissions.Contains(c.Value)))
        {
            var result = await roleManager.RemoveClaimAsync(role, claim);
            if (!result.Succeeded)
                return ToResult(result);
        }

        var existing = current.Select(c => c.Value).ToHashSet();
        foreach (var permission in permissions.Where(p => !existing.Contains(p)))
        {
            var result = await roleManager.AddClaimAsync(
                role,
                new Claim(CustomClaims.Permission, permission)
            );
            if (!result.Succeeded)
                return ToResult(result);
        }

        return OperationResult.Success();
    }

    public async Task<OperationResult> AssignToUserAsync(string username, string roleName)
    {
        var user = await userManager.FindByNameAsync(username);
        if (user is null)
            return OperationResult.Failure($"User '{username}' does not exist.");

        if (await roleManager.FindByNameAsync(roleName) is null)
            return OperationResult.Failure($"Role '{roleName}' does not exist.");

        if (await userManager.IsInRoleAsync(user, roleName))
            return OperationResult.Failure($"User '{username}' is already in role '{roleName}'.");

        return ToResult(await userManager.AddToRoleAsync(user, roleName));
    }

    public async Task<OperationResult> RemoveFromUserAsync(string username, string roleName)
    {
        var user = await userManager.FindByNameAsync(username);
        if (user is null)
            return OperationResult.Failure($"User '{username}' does not exist.");

        if (!await userManager.IsInRoleAsync(user, roleName))
            return OperationResult.Failure($"User '{username}' is not in role '{roleName}'.");

        return ToResult(await userManager.RemoveFromRoleAsync(user, roleName));
    }

    private static OperationResult ToResult(IdentityResult result) =>
        result.Succeeded
            ? OperationResult.Success()
            : OperationResult.Failure(result.Errors.Select(e => e.Description));
}
