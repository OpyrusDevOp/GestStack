namespace GestStack.Application.Common.Models;

public record RoleInfo(string Name, IReadOnlyList<string> Permissions);
