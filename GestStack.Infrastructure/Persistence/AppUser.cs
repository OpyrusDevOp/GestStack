using Microsoft.AspNetCore.Identity;

namespace GestStack.Infrastructure.Persistence;

public class AppUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}
