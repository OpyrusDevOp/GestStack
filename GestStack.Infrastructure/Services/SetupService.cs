using GestStack.Application.Common.Interfaces;
using GestStack.Application.Common.Models;
using GestStack.Application.Common.Security;
using GestStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GestStack.Infrastructure.Services;

public class SetupService(UserManager<AppUser> userManager, AppDbContext dbContext) : ISetupService
{
    public async Task<OperationResult> CreateAdminAsync(
        string username,
        string fullName,
        string password
    )
    {
        var hasAdmin = (await userManager.GetUsersInRoleAsync(Roles.Administrator)).Any();

        if (hasAdmin)
            return OperationResult
                .Failure("An administrator account already exists.")
                .WithErrorCode(SetupErrorCodes.AdminAlreadyExists);

        var user = new AppUser { UserName = username, FullName = fullName };

        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
            return OperationResult.Failure(result.Errors.Select(e => e.Description));

        var roleResult = await userManager.AddToRoleAsync(user, Roles.Administrator);

        if (!roleResult.Succeeded)
            return OperationResult.Failure(roleResult.Errors.Select(e => e.Description));

        await transaction.CommitAsync();

        return OperationResult.Success();
    }

    public async Task<StatusResult> GetStatusAsync()
    {
        var hasAdmin = (await userManager.GetUsersInRoleAsync(Roles.Administrator)).Any();
        var hasProfile = await dbContext.CompanyProfiles.AnyAsync();
        var needSetup = !hasAdmin || !hasProfile;

        return new(needSetup, hasAdmin, hasProfile);
    }
}
