#nullable enable

using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VeraciBot.App.Data;
using VeraciBot.App.Entities;

namespace VeraciBot.App.Auth
{
    public class RoleRequirementAttribute : AuthorizeAttribute
    {
        public RoleRequirementAttribute(params EApplicationRoles[] roles)
        {
            Policy = RolePolicies.For(roles);
        }
    }

    public static class RolePolicies
    {
        private const string Prefix = "ApplicationRole:";

        public static string For(params EApplicationRoles[] roles)
        {
            return Prefix + string.Join(",", roles.Select(x => ((int)x).ToString(CultureInfo.InvariantCulture)));
        }

        public static bool TryParse(string policyName, out EApplicationRoles[] roles)
        {
            roles = [];

            if (!policyName.StartsWith(Prefix, StringComparison.Ordinal))
                return false;

            var roleNames = policyName[Prefix.Length..]
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            roles = roleNames
                .Select(ParseRole)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Distinct()
                .ToArray();

            return true;
        }

        private static EApplicationRoles? ParseRole(string value)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericRole))
                return (EApplicationRoles)numericRole;

            return Enum.TryParse<EApplicationRoles>(value, ignoreCase: true, out var namedRole)
                ? namedRole
                : null;
        }
    }

    public sealed class RoleRequirement : IAuthorizationRequirement
    {
        public RoleRequirement(IEnumerable<EApplicationRoles> roles)
        {
            Roles = roles.Distinct().ToArray();
        }

        public IReadOnlyCollection<EApplicationRoles> Roles { get; }
    }

    public sealed class RoleRequirementHandler : AuthorizationHandler<RoleRequirement>
    {
        private readonly IServiceScopeFactory scopeFactory;

        public RoleRequirementHandler(IServiceScopeFactory scopeFactory)
        {
            this.scopeFactory = scopeFactory;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            RoleRequirement requirement)
        {
            if (context.User.Identity?.IsAuthenticated != true)
                return;

            if (requirement.Roles.Count == 0)
            {
                context.Succeed(requirement);
                return;
            }

            var userRoles = await GetDatabaseRolesAsync(context.User)
                ?? GetClaimRoles(context.User);

            if (userRoles.Count == 0)
                return;

            var maxRole = userRoles.Max();
            if (requirement.Roles.Any(requiredRole => requiredRole <= maxRole))
                context.Succeed(requirement);
        }

        private static bool IsRoleClaim(Claim claim)
        {
            return claim.Type == ClaimTypes.Role ||
                claim.Type.Equals("role", StringComparison.OrdinalIgnoreCase) ||
                claim.Type.Equals("role_name", StringComparison.OrdinalIgnoreCase);
        }

        private static EApplicationRoles? ParseRoleClaim(string value)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericRole))
                return (EApplicationRoles)numericRole;

            return Enum.TryParse<EApplicationRoles>(value, ignoreCase: true, out var namedRole)
                ? namedRole
                : null;
        }

        private static List<EApplicationRoles> GetClaimRoles(ClaimsPrincipal user)
        {
            return user.Claims
                .Where(IsRoleClaim)
                .Select(x => ParseRoleClaim(x.Value))
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Distinct()
                .ToList();
        }

        private async Task<List<EApplicationRoles>?> GetDatabaseRolesAsync(ClaimsPrincipal user)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedUserId))
                return null;

            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var roles = await db.UserRoles
                .Where(userRole => userRole.UserId == parsedUserId)
                .Join(
                    db.Roles,
                    userRole => userRole.RoleId,
                    role => role.Id,
                    (userRole, role) => new
                    {
                        role.Role,
                        RoleName = EF.Property<string?>(role, "Name")
                    })
                .ToListAsync();

            return roles
                .Select(x => x.Role == default ? ParseRoleClaim(x.RoleName ?? string.Empty) : x.Role)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Distinct()
                .ToList();
        }
    }

    public sealed class RoleRequirementPolicyProvider : DefaultAuthorizationPolicyProvider
    {
        public RoleRequirementPolicyProvider(IOptions<AuthorizationOptions> options)
            : base(options)
        {
        }

        public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if (!RolePolicies.TryParse(policyName, out var roles))
                return base.GetPolicyAsync(policyName);

            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new RoleRequirement(roles))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }
    }
}
