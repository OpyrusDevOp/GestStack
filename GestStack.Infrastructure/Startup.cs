using GestStack.Application.Common.Interfaces;
using GestStack.Infrastructure.Identity;
using GestStack.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GestStack.Infrastructure;

public class GestStackInfrastructure
{
    public static async Task StartupSetup(
        IServiceProvider services,
        ILogger logger,
        JwtSettings jwt
    )
    {
        await IdentityDataSeeder.SeedAsync(services);

        var setupService = services.GetRequiredService<ISetupService>();

        var status = await setupService.GetStatusAsync();

        if (!status.NeedSetup)
            return;

        var setupToken = SetupService.GenerateSetupToken(jwt);

        logger.LogWarning(
            "Setup token (valid {Minutes} min): {Token}",
            jwt.ExpiryMinutes,
            setupToken
        );
    }
}
