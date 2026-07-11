using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GestStack.Application.Common.Interfaces;
using GestStack.Application.Common.Models;
using GestStack.Application.Common.Security;
using GestStack.Infrastructure.Identity;
using GestStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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

    public static string GenerateSetupToken(JwtSettings jwt)
    {
        var claims = new List<Claim>
        {
            new(CustomClaims.Setup, "true"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.SetupAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(jwt.ExpiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
