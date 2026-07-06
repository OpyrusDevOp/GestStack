using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GestStack.Application.Common.Security;
using GestStack.Infrastructure.Identity;
using GestStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace GestStack.Tests.Unit.Infrastructure.Identity;

public class AuthServiceTests
{
    private static readonly JwtSettings Settings = new()
    {
        Issuer = "GestStack.Tests",
        Audience = "GestStack.Tests",
        Key = "unit-test-signing-key-0123456789-abcdefghij",
        ExpiryMinutes = 30,
    };

    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly AppDbContext _db;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _userManager = Substitute.For<UserManager<AppUser>>(
            Substitute.For<IUserStore<AppUser>>(),
            null, null, null, null, null, null, null, null);
        _signInManager = Substitute.For<SignInManager<AppUser>>(
            _userManager,
            Substitute.For<IHttpContextAccessor>(),
            Substitute.For<IUserClaimsPrincipalFactory<AppUser>>(),
            null, null, null, null);
        _roleManager = Substitute.For<RoleManager<IdentityRole>>(
            Substitute.For<IRoleStore<IdentityRole>>(),
            null, null, null, null);
        _db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        _userManager.GetClaimsAsync(Arg.Any<AppUser>()).Returns([]);
        _sut = new AuthService(
            _userManager, _signInManager, _roleManager, _db, Options.Create(Settings));
    }

    private async Task<AppUser> CreateLoggableUserAsync(string username = "jdoe")
    {
        var user = new AppUser { UserName = username };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        _userManager.FindByNameAsync(username).Returns(user);
        _userManager.GetRolesAsync(user).Returns([]);
        _signInManager.CheckPasswordSignInAsync(user, "Passw0rd!", true)
            .Returns(SignInResult.Success);
        return user;
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_CreatesUserAndReturnsToken()
    {
        _userManager.CreateAsync(Arg.Any<AppUser>(), "Passw0rd!").Returns(IdentityResult.Success);
        _userManager.GetRolesAsync(Arg.Any<AppUser>()).Returns([]);

        var result = await _sut.RegisterAsync("jdoe", "John Doe", "Passw0rd!");

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Token);
        await _userManager.Received(1).CreateAsync(
            Arg.Is<AppUser>(u => u.UserName == "jdoe" && u.FullName == "John Doe"),
            "Passw0rd!");
    }

    [Fact]
    public async Task RegisterAsync_WhenCreationFails_ReturnsIdentityErrors()
    {
        _userManager.CreateAsync(Arg.Any<AppUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Failed(
                new IdentityError { Description = "Username 'jdoe' is already taken." }));

        var result = await _sut.RegisterAsync("jdoe", "John Doe", "Passw0rd!");

        Assert.False(result.Succeeded);
        Assert.Null(result.Token);
        Assert.Equal(["Username 'jdoe' is already taken."], result.Errors);
    }

    [Fact]
    public async Task LoginAsync_WithUnknownUser_ReturnsGenericErrorWithoutCheckingPassword()
    {
        _userManager.FindByNameAsync("ghost").Returns((AppUser?)null);

        var result = await _sut.LoginAsync("ghost", "whatever");

        Assert.False(result.Succeeded);
        Assert.Equal(["Invalid username or password."], result.Errors);
        await _signInManager.DidNotReceiveWithAnyArgs()
            .CheckPasswordSignInAsync(default(AppUser)!, default!, default);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ReturnsSameGenericError()
    {
        var user = new AppUser { UserName = "jdoe" };
        _userManager.FindByNameAsync("jdoe").Returns(user);
        _signInManager.CheckPasswordSignInAsync(user, "wrong", true)
            .Returns(SignInResult.Failed);

        var result = await _sut.LoginAsync("jdoe", "wrong");

        Assert.False(result.Succeeded);
        Assert.Equal(["Invalid username or password."], result.Errors);
    }

    [Fact]
    public async Task LoginAsync_WhenLockedOut_SaysSo()
    {
        var user = new AppUser { UserName = "jdoe" };
        _userManager.FindByNameAsync("jdoe").Returns(user);
        _signInManager.CheckPasswordSignInAsync(user, "Passw0rd!", true)
            .Returns(SignInResult.LockedOut);

        var result = await _sut.LoginAsync("jdoe", "Passw0rd!");

        Assert.False(result.Succeeded);
        Assert.Equal(["Account is locked out."], result.Errors);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokenWithExpectedClaims()
    {
        var user = new AppUser { UserName = "jdoe" };
        _userManager.FindByNameAsync("jdoe").Returns(user);
        _userManager.GetRolesAsync(user).Returns(["Admin"]);
        _signInManager.CheckPasswordSignInAsync(user, "Passw0rd!", true)
            .Returns(SignInResult.Success);

        var result = await _sut.LoginAsync("jdoe", "Passw0rd!");

        Assert.True(result.Succeeded);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        Assert.Equal(Settings.Issuer, jwt.Issuer);
        Assert.Equal(Settings.Audience, Assert.Single(jwt.Audiences));
        Assert.Equal(user.Id, jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("jdoe", jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.UniqueName).Value);
        Assert.Equal("Admin", jwt.Claims.Single(c => c.Type == ClaimTypes.Role).Value);
        Assert.InRange(
            jwt.ValidTo,
            DateTime.UtcNow.AddMinutes(Settings.ExpiryMinutes - 1),
            DateTime.UtcNow.AddMinutes(Settings.ExpiryMinutes + 1));
    }

    [Fact]
    public async Task LoginAsync_EmbedsRoleAndUserPermissionsInToken()
    {
        var user = new AppUser { UserName = "jdoe" };
        var role = new IdentityRole(Roles.Administrator);
        _userManager.FindByNameAsync("jdoe").Returns(user);
        _userManager.GetRolesAsync(user).Returns([Roles.Administrator]);
        _userManager.GetClaimsAsync(user).Returns([
            new Claim(CustomClaims.Permission, Permissions.Finance.Get),
            new Claim(CustomClaims.Permission, Permissions.Inventory.Modify)]);
        _roleManager.FindByNameAsync(Roles.Administrator).Returns(role);
        _roleManager.GetClaimsAsync(role).Returns([
            new Claim(CustomClaims.Permission, Permissions.Inventory.Modify),
            new Claim(CustomClaims.Permission, Permissions.Procurement.PurchaseRequisitions.Modify),
            new Claim("other-claim", "ignored")]);
        _signInManager.CheckPasswordSignInAsync(user, "Passw0rd!", true)
            .Returns(SignInResult.Success);

        var result = await _sut.LoginAsync("jdoe", "Passw0rd!");

        Assert.True(result.Succeeded);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        var permissions = jwt.Claims
            .Where(c => c.Type == CustomClaims.Permission)
            .Select(c => c.Value)
            .ToArray();
        // deduplicated union of role permissions and user-specific permissions
        Assert.Equivalent(new[]
        {
            Permissions.Inventory.Modify,
            Permissions.Procurement.PurchaseRequisitions.Modify,
            Permissions.Finance.Get,
        }, permissions);
        Assert.DoesNotContain(jwt.Claims, c => c.Type == "other-claim");
    }

    [Fact]
    public async Task LoginAsync_ReturnsRefreshTokenAndStoresOnlyItsHash()
    {
        await CreateLoggableUserAsync();

        var result = await _sut.LoginAsync("jdoe", "Passw0rd!");

        Assert.True(result.Succeeded);
        Assert.NotNull(result.RefreshToken);
        var stored = Assert.Single(_db.RefreshTokens);
        Assert.NotEqual(result.RefreshToken, stored.TokenHash);
        Assert.DoesNotContain(result.RefreshToken, stored.TokenHash);
        Assert.InRange(
            stored.ExpiresAtUtc,
            DateTime.UtcNow.AddDays(Settings.RefreshTokenExpiryDays).AddMinutes(-1),
            DateTime.UtcNow.AddDays(Settings.RefreshTokenExpiryDays).AddMinutes(1));
    }

    [Fact]
    public async Task RefreshAsync_RotatesTokenAndEmbedsCurrentPermissions()
    {
        var user = await CreateLoggableUserAsync();
        var login = await _sut.LoginAsync("jdoe", "Passw0rd!");

        // Permissions granted after login must show up in the refreshed access token.
        var role = new IdentityRole(Roles.Administrator);
        _userManager.GetRolesAsync(user).Returns([Roles.Administrator]);
        _roleManager.FindByNameAsync(Roles.Administrator).Returns(role);
        _roleManager.GetClaimsAsync(role).Returns([
            new Claim(CustomClaims.Permission, Permissions.Inventory.Get)]);

        var refreshed = await _sut.RefreshAsync(login.RefreshToken!);

        Assert.True(refreshed.Succeeded);
        Assert.NotEqual(login.RefreshToken, refreshed.RefreshToken);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(refreshed.Token);
        Assert.Contains(jwt.Claims,
            c => c.Type == CustomClaims.Permission && c.Value == Permissions.Inventory.Get);

        // The old refresh token was rotated out and no longer works.
        var replay = await _sut.RefreshAsync(login.RefreshToken!);
        Assert.False(replay.Succeeded);
    }

    [Fact]
    public async Task RefreshAsync_WithUnknownToken_Fails()
    {
        var result = await _sut.RefreshAsync("not-a-real-token");

        Assert.False(result.Succeeded);
        Assert.Equal(["Invalid refresh token."], result.Errors);
    }

    [Fact]
    public async Task RefreshAsync_WithExpiredToken_Fails()
    {
        var user = await CreateLoggableUserAsync();
        var login = await _sut.LoginAsync("jdoe", "Passw0rd!");
        var stored = _db.RefreshTokens.Single();
        stored.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
        await _db.SaveChangesAsync();

        var result = await _sut.RefreshAsync(login.RefreshToken!);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task RefreshAsync_ReplayOfRevokedToken_RevokesAllUserTokens()
    {
        await CreateLoggableUserAsync();
        var first = await _sut.LoginAsync("jdoe", "Passw0rd!");
        var second = await _sut.LoginAsync("jdoe", "Passw0rd!");
        await _sut.RefreshAsync(first.RefreshToken!); // rotates: first is now revoked

        var replay = await _sut.RefreshAsync(first.RefreshToken!);

        Assert.False(replay.Succeeded);
        Assert.All(_db.RefreshTokens, t => Assert.NotNull(t.RevokedAtUtc));
        var secondUse = await _sut.RefreshAsync(second.RefreshToken!);
        Assert.False(secondUse.Succeeded);
    }

    [Fact]
    public async Task RevokeAsync_MakesTokenUnusable()
    {
        await CreateLoggableUserAsync();
        var login = await _sut.LoginAsync("jdoe", "Passw0rd!");

        await _sut.RevokeAsync(login.RefreshToken!);

        Assert.NotNull(_db.RefreshTokens.Single().RevokedAtUtc);
        var result = await _sut.RefreshAsync(login.RefreshToken!);
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task LoginAsync_ChecksPasswordWithLockoutEnabled()
    {
        var user = new AppUser { UserName = "jdoe" };
        _userManager.FindByNameAsync("jdoe").Returns(user);
        _userManager.GetRolesAsync(user).Returns([]);
        _signInManager.CheckPasswordSignInAsync(user, "Passw0rd!", true)
            .Returns(SignInResult.Success);

        await _sut.LoginAsync("jdoe", "Passw0rd!");

        await _signInManager.Received(1)
            .CheckPasswordSignInAsync(user, "Passw0rd!", lockoutOnFailure: true);
    }
}
