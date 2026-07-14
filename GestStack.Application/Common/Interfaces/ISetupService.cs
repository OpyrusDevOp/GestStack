using GestStack.Application.Common.Models;

namespace GestStack.Application.Common.Interfaces;

public interface ISetupService
{
    Task<StatusResult> GetStatusAsync();
    Task<OperationResult> CreateAdminAsync(string username, string fullName, string password);
    Task<OperationResult> CreateCompanyProfileAsync(
        string legalName,
        string? email,
        string? country,
        byte[]? logo,
        string? logoContentType
    );
}
