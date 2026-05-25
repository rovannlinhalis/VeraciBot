using System.Globalization;
using VeraciBot.Core.Enums;

namespace VeraciBot.Application.Services
{
    public static class RolePolicyNameService
    {
        public const string Prefix = "ApplicationRole:";

        public static string For(params EApplicationRoles[] roles)
        {
            return Prefix + string.Join(",", roles.Select(x => ((int)x).ToString(CultureInfo.InvariantCulture)));
        }

        public static bool TryParse(string policyName, out EApplicationRoles[] roles)
        {
            roles = [];

            if (string.IsNullOrWhiteSpace(policyName) ||
                !policyName.StartsWith(Prefix, StringComparison.Ordinal))
            {
                return false;
            }

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

        public static EApplicationRoles? ParseRole(string value)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericRole))
                return (EApplicationRoles)numericRole;

            return Enum.TryParse<EApplicationRoles>(value, ignoreCase: true, out var namedRole)
                ? namedRole
                : null;
        }
    }
}
