using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GestStack.Infrastructure.Identity;
using GestStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
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
        _sut = new AuthService(_userManager, _signInManager, Options.Create(Settings));
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
