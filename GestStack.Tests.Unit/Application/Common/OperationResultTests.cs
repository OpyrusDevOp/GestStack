using GestStack.Application.Common.Models;

namespace GestStack.Tests.Unit.Application.Common;

public class OperationResultTests
{
    [Fact]
    public void Success_HasNoErrorsAndNoErrorCode()
    {
        var result = OperationResult.Success();

        Assert.True(result.Succeeded);
        Assert.Null(result.ErrorCode);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void WithErrorCode_SetsCodeAndKeepsErrors()
    {
        var result = OperationResult
            .Failure("first error", "second error")
            .WithErrorCode(SetupErrorCodes.AdminAlreadyExists);

        Assert.False(result.Succeeded);
        Assert.Equal(SetupErrorCodes.AdminAlreadyExists, result.ErrorCode);
        Assert.Equal(["first error", "second error"], result.Errors);
    }
}
