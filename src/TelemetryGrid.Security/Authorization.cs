using System.Collections.Concurrent;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace TelemetryGrid.Security;

public interface IUserSecurityStore
{
    string GetStamp(string userId);
    bool IsValid(string userId, string stamp);
    void Revoke(string userId);
}

public sealed class InMemoryUserSecurityStore : IUserSecurityStore
{
    private readonly ConcurrentDictionary<string, string> _stamps = new();

    public string GetStamp(string userId)
        => _stamps.GetOrAdd(userId, _ => Guid.NewGuid().ToString("N"));

    public bool IsValid(string userId, string stamp)
        => _stamps.TryGetValue(userId, out var current) &&
           CryptographicOperations.FixedTimeEquals(
               System.Text.Encoding.UTF8.GetBytes(current),
               System.Text.Encoding.UTF8.GetBytes(stamp));

    public void Revoke(string userId) => _stamps[userId] = Guid.NewGuid().ToString("N");
}

public sealed class SecurityStampCache(IUserSecurityStore store, IMemoryCache cache)
{
    public bool IsValid(string userId, string stamp)
    {
        var key = $"security-stamp:{userId}:{stamp}";
        return cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5);
            return store.IsValid(userId, stamp);
        });
    }
}

public sealed class SecurityStampValidationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, SecurityStampCache cache)
    {
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/telemetry/stream"))
        {
            await next(context);
            return;
        }

        var userId = context.Request.Headers["X-User-Id"].ToString();
        var stamp = context.Request.Headers["X-Security-Stamp"].ToString();
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(stamp) || !cache.IsValid(userId, stamp))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { title = "Unauthorized", detail = "Security stamp is invalid or expired." });
            return;
        }

        await next(context);
    }
}

public sealed class HeaderAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    Microsoft.Extensions.Logging.ILoggerFactory logger,
    System.Text.Encodings.Web.UrlEncoder encoder,
    SecurityStampCache stampCache)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = Request.Headers["X-User-Id"].ToString();
        var stamp = Request.Headers["X-Security-Stamp"].ToString();
        if (string.IsNullOrWhiteSpace(userId) || !stampCache.IsValid(userId, stamp))
            return Task.FromResult(AuthenticateResult.NoResult());

        var permissions = Request.Headers["X-Permissions"].ToString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        claims.AddRange(permissions.Select(permission => new Claim("permission", permission)));
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}

public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
    {
        Permission = permission;
        Policy = $"Permission:{permission}";
    }

    public string Permission { get; }
}

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

public sealed class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.HasClaim("permission", requirement.Permission)) context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith("Permission:", StringComparison.Ordinal)) return base.GetPolicyAsync(policyName);
        var permission = policyName["Permission:".Length..];
        var policy = new AuthorizationPolicyBuilder("TelemetryHeader")
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permission))
            .Build();
        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
