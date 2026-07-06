using System.Security.Claims;
using GestStack.Application.Common.Security;
using GestStack.Infrastructure.Identity;
using GestStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace GestStack.Tests.Unit.Infrastructure.Identity;

public class RoleServiceTests
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleService _sut;

    public RoleServiceTests()
    {
        _roleManager = Substitute.For<RoleManager<IdentityRole>>(
            Substitute.For<IRoleStore<IdentityRole>>(),
            null, null, null, null);
        _userManager = Substitute.For<UserManager<AppUser>>(
            Substitute.For<IUserStore<AppUser>>(),
            null, null, null, null, null, null, null, null);
        _sut = new RoleService(_roleManager, _userManager);
    }

    [Fact]
    public async Task GetRolesAsync_ReturnsRolesWithTheirPermissions()
    {
        var role = new IdentityRole("Buyer");
        _roleManager.Roles.Returns(new[] { role }.AsQueryable());
        _roleManager.GetClaimsAsync(role).Returns([
            new Claim(CustomClaims.Permission, Permissions.Procurement.PurchaseOrders.Get),
            new Claim("other-claim", "ignored")]);

        var roles = await _sut.GetRolesAsync();

        var info = Assert.Single(roles);
        Assert.Equal("Buyer", info.Name);
        Assert.Equal([Permissions.Procurement.PurchaseOrders.Get], info.Permissions);
    }

    [Fact]
    public async Task CreateRoleAsync_WhenRoleAlreadyExists_Fails()
    {
        _roleManager.FindByNameAsync("Buyer").Returns(new IdentityRole("Buyer"));

        var result = await _sut.CreateRoleAsync("Buyer");

        Assert.False(result.Succeeded);
        await _roleManager.DidNotReceiveWithAnyArgs().CreateAsync(default!);
    }

    [Fact]
    public async Task DeleteRoleAsync_Administrator_IsBlocked()
    {
        var result = await _sut.DeleteRoleAsync(Roles.Administrator);

        Assert.False(result.Succeeded);
        await _roleManager.DidNotReceiveWithAnyArgs().DeleteAsync(default!);
    }

    [Fact]
    public async Task SetPermissionsAsync_Administrator_IsBlocked()
    {
        var result = await _sut.SetPermissionsAsync(Roles.Administrator, [Permissions.Inventory.Get]);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task SetPermissionsAsync_WithUnknownPermission_FailsWithoutTouchingRole()
    {
        var result = await _sut.SetPermissionsAsync("Buyer", ["inventory:get", "not:a:permission"]);

        Assert.False(result.Succeeded);
        Assert.Equal(["Unknown permission 'not:a:permission'."], result.Errors);
        await _roleManager.DidNotReceiveWithAnyArgs().AddClaimAsync(default!, default!);
        await _roleManager.DidNotReceiveWithAnyArgs().RemoveClaimAsync(default!, default!);
    }

    [Fact]
    public async Task SetPermissionsAsync_AddsMissingAndRemovesExtraClaims()
    {
        var role = new IdentityRole("Buyer");
        _roleManager.FindByNameAsync("Buyer").Returns(role);
        _roleManager.GetClaimsAsync(role).Returns([
            new Claim(CustomClaims.Permission, Permissions.Inventory.Get),
            new Claim(CustomClaims.Permission, Permissions.Inventory.Create)]);
        _roleManager.AddClaimAsync(role, Arg.Any<Claim>()).Returns(IdentityResult.Success);
        _roleManager.RemoveClaimAsync(role, Arg.Any<Claim>()).Returns(IdentityResult.Success);

        var result = await _sut.SetPermissionsAsync(
            "Buyer", [Permissions.Inventory.Create, Permissions.Finance.Get]);

        Assert.True(result.Succeeded);
        await _roleManager.Received(1).RemoveClaimAsync(
            role, Arg.Is<Claim>(c => c.Value == Permissions.Inventory.Get));
        await _roleManager.Received(1).AddClaimAsync(
            role, Arg.Is<Claim>(c => c.Value == Permissions.Finance.Get));
        await _roleManager.DidNotReceive().AddClaimAsync(
            role, Arg.Is<Claim>(c => c.Value == Permissions.Inventory.Create));
    }

    [Fact]
    public async Task AssignToUserAsync_WithUnknownUser_Fails()
    {
        _userManager.FindByNameAsync("ghost").Returns((AppUser?)null);

        var result = await _sut.AssignToUserAsync("ghost", "Buyer");

        Assert.False(result.Succeeded);
        Assert.Equal(["User 'ghost' does not exist."], result.Errors);
    }

    [Fact]
    public async Task AssignToUserAsync_AddsUserToRole()
    {
        var user = new AppUser { UserName = "jdoe" };
        _userManager.FindByNameAsync("jdoe").Returns(user);
        _roleManager.FindByNameAsync("Buyer").Returns(new IdentityRole("Buyer"));
        _userManager.IsInRoleAsync(user, "Buyer").Returns(false);
        _userManager.AddToRoleAsync(user, "Buyer").Returns(IdentityResult.Success);

        var result = await _sut.AssignToUserAsync("jdoe", "Buyer");

        Assert.True(result.Succeeded);
        await _userManager.Received(1).AddToRoleAsync(user, "Buyer");
    }

    [Fact]
    public async Task RemoveFromUserAsync_WhenUserNotInRole_Fails()
    {
        var user = new AppUser { UserName = "jdoe" };
        _userManager.FindByNameAsync("jdoe").Returns(user);
        _userManager.IsInRoleAsync(user, "Buyer").Returns(false);

        var result = await _sut.RemoveFromUserAsync("jdoe", "Buyer");

        Assert.False(result.Succeeded);
        await _userManager.DidNotReceiveWithAnyArgs().RemoveFromRoleAsync(default!, default!);
    }
}
