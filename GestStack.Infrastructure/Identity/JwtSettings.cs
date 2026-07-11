namespace GestStack.Infrastructure.Identity;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public required string Issuer { get; set; }
    public required string Audience { get; set; }
    public required string SetupAudience { get; set; }
    public required string Key { get; set; }
    public int ExpiryMinutes { get; set; } = 15;
    public int RefreshTokenExpiryDays { get; set; } = 7;
}
