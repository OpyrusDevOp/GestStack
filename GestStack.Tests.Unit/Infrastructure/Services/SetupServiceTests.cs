using System.IdentityModel.Tokens.Jwt;
using GestStack.Application.Common.Models;
using GestStack.Application.Common.Security;
using GestStack.Infrastructure.Identity;
using GestStack.Infrastructure.Persistence;
using GestStack.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NSubstitute;

namespace GestStack.Tests.Unit.Infrastructure.Services;

public class SetupServiceTests
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _db;
    private readonly SetupService _sut;

    public SetupServiceTests()
    {
        _userManager = Substitute.For<UserManager<AppUser>>(
            Substitute.For<IUserStore<AppUser>>(),
            null, null, null, null, null, null, null, null);
        _db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);
        _userManager.GetUsersInRoleAsync(Roles.Administrator).Returns([]);
        _sut = new SetupService(_userManager, _db);
    }

    private void GivenAnAdminExists() =>
        _userManager.GetUsersInRoleAsync(Roles.Administrator)
            .Returns([new AppUser { UserName = "admin" }]);

    private async Task GivenACompanyProfileExists()
    {
        _db.CompanyProfiles.Add(new Domain.Entities.CompanyProfile { LegalName = "Acme Corp" });
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetStatusAsync_WithNothingConfigured_NeedsSetup()
    {
        var status = await _sut.GetStatusAsync();

        Assert.True(status.NeedSetup);
        Assert.False(status.HasAdmin);
        Assert.False(status.HasCompanyProfile);
    }

    [Fact]
    public async Task GetStatusAsync_WithAdminButNoProfile_StillNeedsSetup()
    {
        GivenAnAdminExists();

        var status = await _sut.GetStatusAsync();

        Assert.True(status.NeedSetup);
        Assert.True(status.HasAdmin);
        Assert.False(status.HasCompanyProfile);
    }

    [Fact]
    public async Task GetStatusAsync_WithAdminAndProfile_DoesNotNeedSetup()
    {
        GivenAnAdminExists();
        await GivenACompanyProfileExists();

        var status = await _sut.GetStatusAsync();

        Assert.False(status.NeedSetup);
        Assert.True(status.HasAdmin);
        Assert.True(status.HasCompanyProfile);
    }

    [Fact]
    public async Task CreateAdminAsync_WithValidData_CreatesUserInAdministratorRole()
    {
        _userManager.CreateAsync(Arg.Any<AppUser>(), "Passw0rd!").Returns(IdentityResult.Success);
        _userManager.AddToRoleAsync(Arg.Any<AppUser>(), Roles.Administrator)
            .Returns(IdentityResult.Success);

        var result = await _sut.CreateAdminAsync("admin", "Ada Min", "Passw0rd!");

        Assert.True(result.Succeeded);
        await _userManager.Received(1).CreateAsync(
            Arg.Is<AppUser>(u => u.UserName == "admin" && u.FullName == "Ada Min"),
            "Passw0rd!");
        await _userManager.Received(1)
            .AddToRoleAsync(Arg.Is<AppUser>(u => u.UserName == "admin"), Roles.Administrator);
    }

    [Fact]
    public async Task CreateAdminAsync_WhenAdminAlreadyExists_FailsWithConflictCode()
    {
        GivenAnAdminExists();

        var result = await _sut.CreateAdminAsync("second", "Second Admin", "Passw0rd!");

        Assert.False(result.Succeeded);
        Assert.Equal(SetupErrorCodes.AdminAlreadyExists, result.ErrorCode);
        await _userManager.DidNotReceiveWithAnyArgs().CreateAsync(default!, default!);
    }

    [Fact]
    public async Task CreateAdminAsync_WhenCreationFails_ReturnsErrorsAndSkipsRoleAssignment()
    {
        _userManager.CreateAsync(Arg.Any<AppUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Failed(
                new IdentityError { Description = "Password is too weak." }));

        var result = await _sut.CreateAdminAsync("admin", "Ada Min", "weak");

        Assert.False(result.Succeeded);
        Assert.Null(result.ErrorCode);
        Assert.Equal(["Password is too weak."], result.Errors);
        await _userManager.DidNotReceiveWithAnyArgs().AddToRoleAsync(default!, default!);
    }

    [Fact]
    public async Task CreateAdminAsync_WhenRoleAssignmentFails_ReturnsErrors()
    {
        _userManager.CreateAsync(Arg.Any<AppUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Success);
        _userManager.AddToRoleAsync(Arg.Any<AppUser>(), Roles.Administrator)
            .Returns(IdentityResult.Failed(
                new IdentityError { Description = "Role does not exist." }));

        var result = await _sut.CreateAdminAsync("admin", "Ada Min", "Passw0rd!");

        Assert.False(result.Succeeded);
        Assert.Equal(["Role does not exist."], result.Errors);
    }

    [Fact]
    public async Task CreateCompanyProfileAsync_WithValidData_PersistsProfile()
    {
        var result = await _sut.CreateCompanyProfileAsync(
            "Acme Corp", "contact@acme.test", "Benin", [1, 2, 3], "image/png");

        Assert.True(result.Succeeded);
        var profile = Assert.Single(_db.CompanyProfiles);
        Assert.Equal("Acme Corp", profile.LegalName);
        Assert.Equal("contact@acme.test", profile.Email);
        Assert.Equal("Benin", profile.Country);
        Assert.Equal([1, 2, 3], profile.Logo);
        Assert.Equal("image/png", profile.LogoContentType);
    }

    [Fact]
    public async Task CreateCompanyProfileAsync_WithOnlyLegalName_Succeeds()
    {
        var result = await _sut.CreateCompanyProfileAsync("Acme Corp", null, null, null, null);

        Assert.True(result.Succeeded);
        var profile = Assert.Single(_db.CompanyProfiles);
        Assert.Equal("Acme Corp", profile.LegalName);
        Assert.Null(profile.Email);
        Assert.Null(profile.Logo);
    }

    [Fact]
    public async Task CreateCompanyProfileAsync_WhenProfileExists_FailsWithConflictCode()
    {
        await GivenACompanyProfileExists();

        var result = await _sut.CreateCompanyProfileAsync(
            "Other Corp", null, null, null, null);

        Assert.False(result.Succeeded);
        Assert.Equal(SetupErrorCodes.CompanyProfileAlreadyExists, result.ErrorCode);
        var profile = Assert.Single(_db.CompanyProfiles);
        Assert.Equal("Acme Corp", profile.LegalName);
    }

    [Fact]
    public void GenerateSetupToken_TargetsSetupAudienceWithSetupClaim()
    {
        var settings = new JwtSettings
        {
            Issuer = "GestStack.Tests",
            Audience = "GestStack.Tests",
            SetupAudience = "GestStack.Tests-Setup",
            Key = "unit-test-signing-key-0123456789-abcdefghij",
            ExpiryMinutes = 15,
        };

        var token = SetupService.GenerateSetupToken(settings);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal(settings.Issuer, jwt.Issuer);
        // Must carry the setup audience, not the normal API audience,
        // so the default bearer scheme rejects it.
        Assert.Equal(settings.SetupAudience, Assert.Single(jwt.Audiences));
        Assert.Equal("true", jwt.Claims.Single(c => c.Type == CustomClaims.Setup).Value);
        Assert.InRange(
            jwt.ValidTo,
            DateTime.UtcNow.AddMinutes(settings.ExpiryMinutes - 1),
            DateTime.UtcNow.AddMinutes(settings.ExpiryMinutes + 1));
    }
}
