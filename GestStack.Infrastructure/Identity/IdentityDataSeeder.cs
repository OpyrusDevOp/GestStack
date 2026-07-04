using System.Security.Claims;
using GestStack.Application.Common.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace GestStack.Infrastructure.Identity;

public static class IdentityDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var roleName in Roles.All)
            if (await roleManager.FindByNameAsync(roleName) is null)
                await roleManager.CreateAsync(new IdentityRole(roleName));

        var administrator = (await roleManager.FindByNameAsync(Roles.Administrator))!;
        var existing = (await roleManager.GetClaimsAsync(administrator))
            .Where(c => c.Type == CustomClaims.Permission)
            .Select(c => c.Value)
            .ToHashSet();

        foreach (var permission in Permissions.All.Where(p => !existing.Contains(p)))
            await roleManager.AddClaimAsync(
                administrator,
                new Claim(CustomClaims.Permission, permission)
            );
    }
}
