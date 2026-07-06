using Microsoft.AspNetCore.Authorization;

namespace GestStack.API.Authorization;

public sealed class HasPermissionAttribute(string permission) : AuthorizeAttribute(permission);
