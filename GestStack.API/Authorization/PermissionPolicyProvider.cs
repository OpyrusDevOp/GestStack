using GestStack.Application.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace GestStack.API.Authorization;

/// <summary>
/// Resolves unknown policy names that look like permissions ("resource:action")
/// into policies requiring the matching permission claim, so permissions never
/// need to be registered one by one.
/// </summary>
public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var policy = await base.GetPolicyAsync(policyName);
        if (policy is not null || !policyName.Contains(':'))
            return policy;

        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireClaim(CustomClaims.Permission, policyName)
            .Build();
    }
}
