using GestStack.Application.Common.Models;

namespace GestStack.Tests.Unit.Application.Common;

public class AuthResultTests
{
    [Fact]
    public void Success_SetsTokensAndSucceeded()
    {
        var result = AuthResult.Success("some-token", "some-refresh-token");

        Assert.True(result.Succeeded);
        Assert.Equal("some-token", result.Token);
        Assert.Equal("some-refresh-token", result.RefreshToken);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Failure_CollectsErrorsAndDoesNotSucceed()
    {
        var result = AuthResult.Failure("first error", "second error");

        Assert.False(result.Succeeded);
        Assert.Null(result.Token);
        Assert.Null(result.RefreshToken);
        Assert.Equal(["first error", "second error"], result.Errors);
    }
}
