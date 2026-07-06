using System.Security.Claims;
using GestStack.Application.Common.Security;
using GestStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
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

        await SeedAdminUserAsync(services);
    }

    private static async Task SeedAdminUserAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        if ((await userManager.GetUsersInRoleAsync(Roles.Administrator)).Count > 0)
            return;

        var configuration = services.GetRequiredService<IConfiguration>();
        var username = configuration["Seed:AdminUsername"] ?? "admin";
        var password = configuration["Seed:AdminPassword"];
        if (string.IsNullOrEmpty(password))
            return; // No bootstrap password configured; an administrator must be created manually.

        var admin = await userManager.FindByNameAsync(username);
        if (admin is null)
        {
            admin = new AppUser { UserName = username, FullName = "Administrator" };
            var result = await userManager.CreateAsync(admin, password);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    "Failed to seed admin user: "
                        + string.Join(" ", result.Errors.Select(e => e.Description))
                );
        }

        await userManager.AddToRoleAsync(admin, Roles.Administrator);
    }
}
