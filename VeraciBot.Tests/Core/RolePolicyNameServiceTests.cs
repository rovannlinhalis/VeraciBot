using FluentAssertions;
using VeraciBot.Application.Services;
using VeraciBot.Core.Enums;

namespace VeraciBot.Tests.Core
{
    public class RolePolicyNameServiceTests
    {
        [Fact]
        public void For_ShouldCreateStablePolicyName()
        {
            var policy = RolePolicyNameService.For(EApplicationRoles.User, EApplicationRoles.Admin);

            policy.Should().Be("ApplicationRole:1,9");
        }

        [Fact]
        public void TryParse_ShouldIgnoreInvalidRolesAndDeduplicate()
        {
            var parsed = RolePolicyNameService.TryParse("ApplicationRole:9,Admin,invalid", out var roles);

            parsed.Should().BeTrue();
            roles.Should().BeEquivalentTo([EApplicationRoles.Admin]);
        }

        [Fact]
        public void TryParse_ShouldRejectNonRolePolicy()
        {
            var parsed = RolePolicyNameService.TryParse("OtherPolicy:9", out var roles);

            parsed.Should().BeFalse();
            roles.Should().BeEmpty();
        }
    }
}
