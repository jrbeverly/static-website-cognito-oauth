using System.Security.Claims;
using PublishingApi.Domain.Models;

namespace PublishingApi.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static UserContext ToUserContext(this ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue("sub")
            ?? throw new InvalidOperationException("JWT missing 'sub' claim");
        var email = principal.FindFirstValue("email") ?? string.Empty;
        return new UserContext { Sub = sub, Email = email };
    }
}
