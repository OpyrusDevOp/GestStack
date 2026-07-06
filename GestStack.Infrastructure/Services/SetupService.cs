using GestStack.Application.Common.Interfaces;
using GestStack.Application.Common.Models;
using GestStack.Application.Common.Security;
using GestStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GestStack.Infrastructure.Services;

public class SetupService(UserManager<AppUser> userManager, AppDbContext dbContext) : ISetupService
{
    public async Task<StatusResult> GetStatusAsync()
    {
        var hasAdmin = (await userManager.GetUsersInRoleAsync(Roles.Administrator)).Any();
        var hasProfile = await dbContext.CompanyProfiles.AnyAsync();
        var needSetup = !hasAdmin || !hasProfile;

        return new(needSetup, hasAdmin, hasProfile);
    }
}
