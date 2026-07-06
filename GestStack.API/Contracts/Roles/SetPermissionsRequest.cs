namespace GestStack.API.Contracts.Roles;

public record SetPermissionsRequest(IReadOnlyList<string> Permissions);
