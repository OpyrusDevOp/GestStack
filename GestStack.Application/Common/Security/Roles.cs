namespace GestStack.Application.Common.Security;

public static class Roles
{
    public const string Administrator = "Administrator";

    public static IReadOnlyList<string> All { get; } = [Administrator];
}
