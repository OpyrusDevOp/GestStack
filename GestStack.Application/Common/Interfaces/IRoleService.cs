using GestStack.Application.Common.Models;

namespace GestStack.Application.Common.Interfaces;

public interface IRoleService
{
    Task<IReadOnlyList<RoleInfo>> GetRolesAsync();
    Task<OperationResult> CreateRoleAsync(string name);
    Task<OperationResult> DeleteRoleAsync(string name);
    Task<OperationResult> SetPermissionsAsync(string roleName, IReadOnlyCollection<string> permissions);
    Task<OperationResult> AssignToUserAsync(string username, string roleName);
    Task<OperationResult> RemoveFromUserAsync(string username, string roleName);
}
