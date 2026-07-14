using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GestStack.Application.Common.Interfaces;
using GestStack.Application.Common.Models;
using GestStack.Application.Common.Security;
using GestStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GestStack.Infrastructure.Identity;

public class AuthService(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    RoleManager<IdentityRole> roleManager,
    AppDbContext dbContext,
    IOptions<JwtSettings> jwtOptions
) : IAuthService
{
    private readonly JwtSettings _jwt = jwtOptions.Value;

    public async Task<AuthResult> RegisterAsync(string username, string fullName, string password)
    {
        var user = new AppUser { UserName = username, FullName = fullName };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return AuthResult.Failure(result.Errors.Select(e => e.Description));

        return AuthResult.Success(
            await GenerateTokenAsync(user),
            await IssueRefreshTokenAsync(user)
        );
    }

    public async Task<AuthResult> LoginAsync(string username, string password)
    {
        var user = await userManager.FindByNameAsync(username);
        if (user is null)
            return AuthResult.Failure("Invalid username or password.");

        var result = await signInManager.CheckPasswordSignInAsync(
            user,
            password,
            lockoutOnFailure: true
        );
        if (!result.Succeeded)
            return AuthResult.Failure(
                result.IsLockedOut ? "Account is locked out." : "Invalid username or password."
            );

        return AuthResult.Success(
            await GenerateTokenAsync(user),
            await IssueRefreshTokenAsync(user)
        );
    }

    public async Task<AuthResult> RefreshAsync(string refreshToken)
    {
        var stored = await FindRefreshTokenAsync(refreshToken);
        if (stored?.User is null)
            return AuthResult.Failure("Invalid refresh token.");

        if (stored.RevokedAtUtc is not null)
        {
            // A revoked token being replayed suggests it was stolen; cut off the whole session family.
            var activeTokens = await dbContext
                .RefreshTokens.Where(t => t.UserId == stored.UserId && t.RevokedAtUtc == null)
                .ToListAsync();
            foreach (var token in activeTokens)
                token.RevokedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();

            return AuthResult.Failure("Invalid refresh token.");
        }

        if (!stored.IsActive)
            return AuthResult.Failure("Invalid refresh token.");

        stored.RevokedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return AuthResult.Success(
            await GenerateTokenAsync(stored.User),
            await IssueRefreshTokenAsync(stored.User)
        );
    }

    public async Task RevokeAsync(string refreshToken)
    {
        var stored = await FindRefreshTokenAsync(refreshToken);
        if (stored is null || stored.RevokedAtUtc is not null)
            return;

        stored.RevokedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
    }

    private Task<RefreshToken?> FindRefreshTokenAsync(string refreshToken)
    {
        var hash = HashToken(refreshToken);
        return dbContext
            .RefreshTokens.Include(t => t.User)
            .SingleOrDefaultAsync(t => t.TokenHash == hash);
    }

    private async Task<string> IssueRefreshTokenAsync(AppUser user)
    {
        var rawToken = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));

        dbContext.RefreshTokens.Add(
            new RefreshToken
            {
                UserId = user.Id,
                TokenHash = HashToken(rawToken),
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwt.RefreshTokenExpiryDays),
            }
        );
        await dbContext.SaveChangesAsync();

        return rawToken;
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private async Task<string> GenerateTokenAsync(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var roles = await userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        claims.AddRange(
            (await GetPermissionsAsync(user, roles)).Select(p => new Claim(
                CustomClaims.Permission,
                p
            ))
        );

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key)),
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwt.ExpiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<HashSet<string>> GetPermissionsAsync(AppUser user, IList<string> roles)
    {
        var permissions = new HashSet<string>();

        foreach (var roleName in roles)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
                continue;

            foreach (var claim in await roleManager.GetClaimsAsync(role))
                if (claim.Type == CustomClaims.Permission)
                    permissions.Add(claim.Value);
        }

        foreach (var claim in await userManager.GetClaimsAsync(user))
            if (claim.Type == CustomClaims.Permission)
                permissions.Add(claim.Value);

        return permissions;
    }
}
