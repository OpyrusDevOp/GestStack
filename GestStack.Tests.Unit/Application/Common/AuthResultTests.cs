using GestStack.Application.Common.Models;

namespace GestStack.Tests.Unit.Application.Common;

public class AuthResultTests
{
    [Fact]
    public void Success_SetsTokenAndSucceeded()
    {
        var result = AuthResult.Success("some-token");

        Assert.True(result.Succeeded);
        Assert.Equal("some-token", result.Token);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Failure_CollectsErrorsAndDoesNotSucceed()
    {
        var result = AuthResult.Failure("first error", "second error");

        Assert.False(result.Succeeded);
        Assert.Null(result.Token);
        Assert.Equal(["first error", "second error"], result.Errors);
    }
}
