using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VeraciBot.App.Data;
using VeraciBot.Core.Entities;
using Microsoft.Extensions.Logging;
using VeraciBot.Application.External;

namespace VeraciBot.Application.Services
{
    public sealed class TwitterUserAuthorizationService
    {
        private const string TwitterLoginProvider = "Twitter";

        private readonly ApplicationDbContext _db;
        private readonly TwitterAPI _twitterApi;
        private readonly ILogger<TwitterUserAuthorizationService> _logger;

        public TwitterUserAuthorizationService(
            ApplicationDbContext db,
            TwitterAPI twitterApi,
            ILogger<TwitterUserAuthorizationService> logger)
        {
            _db = db;
            _twitterApi = twitterApi;
            _logger = logger;
        }

        public static string NormalizeUsername(string username)
        {
            return string.IsNullOrWhiteSpace(username)
                ? string.Empty
                : username.Trim().TrimStart('@');
        }

        public static bool ApplyExternalLoginInfo(ApplicationUser user, ExternalLoginInfo loginInfo)
        {
            if (user is null || loginInfo is null ||
                !string.Equals(loginInfo.LoginProvider, TwitterLoginProvider, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var changed = false;
            var authorId = FirstClaimValue(
                loginInfo.Principal,
                ClaimTypes.NameIdentifier,
                "urn:twitter:userid",
                "urn:twitter:user_id",
                "user_id",
                "sub");

            if (string.IsNullOrWhiteSpace(authorId))
                authorId = loginInfo.ProviderKey;

            var username = NormalizeUsername(FirstClaimValue(
                loginInfo.Principal,
                "urn:twitter:screenname",
                "urn:twitter:screen_name",
                "screen_name",
                "username",
                "preferred_username",
                "nickname",
                ClaimTypes.Name));

            if (!string.IsNullOrWhiteSpace(authorId) &&
                !string.Equals(user.AuthorId, authorId, StringComparison.Ordinal))
            {
                user.AuthorId = authorId;
                changed = true;
            }

            if (!string.IsNullOrWhiteSpace(username) &&
                !string.Equals(user.TwitterUsername, username, StringComparison.OrdinalIgnoreCase))
            {
                user.TwitterUsername = username;
                changed = true;
            }

            return changed;
        }

        public async Task<TwitterAuthorizationChangeResult> SetApplicationUserAuthorizationAsync(
            ApplicationUser user,
            string requestedStatus,
            long? changedByApplicationUserId,
            string reason,
            CancellationToken cancellationToken = default)
        {
            if (user is null)
                return TwitterAuthorizationChangeResult.Fail("Usuario nao encontrado.");

            if (string.IsNullOrWhiteSpace(requestedStatus))
                return TwitterAuthorizationChangeResult.Ok("Autorizacao do Twitter sem alteracao.");

            if (!IsManageableStatus(requestedStatus))
                return TwitterAuthorizationChangeResult.Fail("Status de autorizacao do Twitter invalido.");

            var authorId = user.AuthorId?.Trim() ?? string.Empty;
            var username = NormalizeUsername(user.TwitterUsername);
            var name = string.Empty;

            if (string.IsNullOrWhiteSpace(authorId) && !string.IsNullOrWhiteSpace(username))
            {
                try
                {
                    var twitterUser = await _twitterApi.GetTwitterUserByUserName(username);
                    if (twitterUser is not null)
                    {
                        authorId = twitterUser.Id;
                        username = NormalizeUsername(twitterUser.Username);
                        name = twitterUser.Name ?? string.Empty;

                        user.AuthorId = authorId;
                        user.TwitterUsername = username;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Nao foi possivel resolver o AuthorId do Twitter para @{Username}.", username);
                    return TwitterAuthorizationChangeResult.Fail(
                        $"Nao foi possivel resolver o AuthorId do Twitter para @{username}: {ex.Message}");
                }
            }

            if (string.IsNullOrWhiteSpace(authorId))
            {
                return TwitterAuthorizationChangeResult.Fail(
                    "Informe o @ do Twitter ou o AuthorId antes de permitir/bloquear o usuario no bot.");
            }

            await SetAuthorizationAsync(
                authorId,
                username,
                name,
                requestedStatus,
                changedByAuthorId: string.Empty,
                applicationUserId: user.Id,
                changedByApplicationUserId: changedByApplicationUserId,
                reason: reason,
                cancellationToken: cancellationToken);

            return TwitterAuthorizationChangeResult.Ok(
                requestedStatus == AuthorizedTwitterUser.STATUS_AUTHORIZED
                    ? "Usuario permitido no bot."
                    : "Usuario bloqueado no bot.");
        }

        public async Task<TwitterInviteCreditChangeResult> AddInviteCreditsAsync(
            ApplicationUser user,
            int amount,
            long? changedByApplicationUserId,
            string reason,
            CancellationToken cancellationToken = default)
        {
            if (user is null)
                return TwitterInviteCreditChangeResult.Fail("Usuario nao encontrado.");

            if (amount <= 0)
                return TwitterInviteCreditChangeResult.Fail("Informe uma quantidade positiva de convites.");

            var authorId = user.AuthorId?.Trim() ?? string.Empty;
            var username = NormalizeUsername(user.TwitterUsername);
            var name = string.Empty;

            if (string.IsNullOrWhiteSpace(authorId) && !string.IsNullOrWhiteSpace(username))
            {
                try
                {
                    var twitterUser = await _twitterApi.GetTwitterUserByUserName(username);
                    if (twitterUser is not null)
                    {
                        authorId = twitterUser.Id;
                        username = NormalizeUsername(twitterUser.Username);
                        name = twitterUser.Name ?? string.Empty;

                        user.AuthorId = authorId;
                        user.TwitterUsername = username;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Nao foi possivel resolver o AuthorId do Twitter para @{Username}.", username);
                    return TwitterInviteCreditChangeResult.Fail(
                        $"Nao foi possivel resolver o AuthorId do Twitter para @{username}: {ex.Message}");
                }
            }

            if (string.IsNullOrWhiteSpace(authorId))
                return TwitterInviteCreditChangeResult.Fail("Informe o @ do Twitter ou o AuthorId antes de adicionar convites.");

            var now = DateTime.UtcNow;
            var current = await _db.AuthorizedTwitterUsers
                .FirstOrDefaultAsync(x => x.AuthorId == authorId, cancellationToken);

            if (current is null)
            {
                current = new AuthorizedTwitterUser
                {
                    AuthorId = authorId,
                    Username = username,
                    Name = name,
                    Status = AuthorizedTwitterUser.STATUS_NOT_AUTHORIZED,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };
                _db.AuthorizedTwitterUsers.Add(current);
            }
            else
            {
                current.Username = !string.IsNullOrWhiteSpace(username) ? username : current.Username;
                current.Name = !string.IsNullOrWhiteSpace(name) ? name : current.Name;
                current.UpdatedAtUtc = now;
            }

            current.InviteCredits += amount;

            _db.TwitterInviteCreditTransactions.Add(new TwitterInviteCreditTransaction
            {
                AuthorId = current.AuthorId,
                Username = current.Username,
                Delta = amount,
                BalanceAfter = current.InviteCredits,
                CreatedAtUtc = now,
                ChangedByApplicationUserId = changedByApplicationUserId,
                Reason = reason
            });

            await _db.SaveChangesAsync(cancellationToken);

            return TwitterInviteCreditChangeResult.Ok(
                $"Foram adicionados {amount} convite(s). Saldo atual: {current.InviteCredits}.",
                current.InviteCredits);
        }

        public async Task<AuthorizedTwitterUser> SetAuthorizationAsync(
            string authorId,
            string username,
            string name,
            string status,
            string changedByAuthorId = "",
            long? applicationUserId = null,
            long? changedByApplicationUserId = null,
            string reason = "",
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(authorId))
                throw new InvalidOperationException("AuthorId do Twitter e obrigatorio para alterar autorizacao.");

            if (!IsKnownStatus(status))
                throw new InvalidOperationException($"Status de autorizacao do Twitter invalido: {status}");

            var now = DateTime.UtcNow;
            var normalizedUsername = NormalizeUsername(username);
            var current = await _db.AuthorizedTwitterUsers
                .FirstOrDefaultAsync(x => x.AuthorId == authorId, cancellationToken);
            var previousStatus = current?.Status ?? string.Empty;

            if (current is null)
            {
                current = new AuthorizedTwitterUser
                {
                    AuthorId = authorId,
                    CreatedAtUtc = now,
                    Score = 0,
                    Wins = 0,
                    Losses = 0
                };
                _db.AuthorizedTwitterUsers.Add(current);
            }

            current.Username = !string.IsNullOrWhiteSpace(normalizedUsername)
                ? normalizedUsername
                : current.Username;
            current.Name = !string.IsNullOrWhiteSpace(name)
                ? name
                : current.Name;
            current.AuthorizedById = !string.IsNullOrWhiteSpace(changedByAuthorId)
                ? changedByAuthorId
                : current.AuthorizedById;
            current.Status = status;
            current.UpdatedAtUtc = now;

            if (status == AuthorizedTwitterUser.STATUS_AUTHORIZED)
            {
                current.AuthorizationDate = now;
                current.DeauthorizationDate = null;
            }
            else if (status == AuthorizedTwitterUser.STATUS_NOT_AUTHORIZED)
            {
                current.DeauthorizationDate = now;
            }
            else if (status == AuthorizedTwitterUser.STATUS_INVITED)
            {
                current.AuthorizationDate = now;
                current.DeauthorizationDate = null;
            }

            if (!string.Equals(previousStatus, status, StringComparison.OrdinalIgnoreCase))
            {
                _db.AuthorizedTwitterUserHistory.Add(new AuthorizedTwitterUserHistory
                {
                    AuthorId = authorId,
                    Username = current.Username,
                    Name = current.Name,
                    PreviousStatus = previousStatus,
                    Status = status,
                    ChangedAtUtc = now,
                    ApplicationUserId = applicationUserId,
                    ChangedByApplicationUserId = changedByApplicationUserId,
                    ChangedByAuthorId = changedByAuthorId,
                    Reason = reason
                });
            }

            await _db.SaveChangesAsync(cancellationToken);
            return current;
        }

        private static bool IsManageableStatus(string status)
        {
            return string.Equals(status, AuthorizedTwitterUser.STATUS_AUTHORIZED, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, AuthorizedTwitterUser.STATUS_NOT_AUTHORIZED, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsKnownStatus(string status)
        {
            return IsManageableStatus(status) ||
                   string.Equals(status, AuthorizedTwitterUser.STATUS_INVITED, StringComparison.OrdinalIgnoreCase);
        }

        private static string FirstClaimValue(ClaimsPrincipal principal, params string[] claimTypes)
        {
            foreach (var claimType in claimTypes)
            {
                var value = principal.FindFirstValue(claimType);
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return string.Empty;
        }
    }

    public sealed class TwitterAuthorizationChangeResult
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; } = string.Empty;

        public static TwitterAuthorizationChangeResult Ok(string message)
        {
            return new TwitterAuthorizationChangeResult
            {
                Succeeded = true,
                Message = message
            };
        }

        public static TwitterAuthorizationChangeResult Fail(string message)
        {
            return new TwitterAuthorizationChangeResult
            {
                Succeeded = false,
                Message = message
            };
        }
    }

    public sealed class TwitterInviteCreditChangeResult
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; } = string.Empty;
        public int BalanceAfter { get; set; }

        public static TwitterInviteCreditChangeResult Ok(string message, int balanceAfter)
        {
            return new TwitterInviteCreditChangeResult
            {
                Succeeded = true,
                Message = message,
                BalanceAfter = balanceAfter
            };
        }

        public static TwitterInviteCreditChangeResult Fail(string message)
        {
            return new TwitterInviteCreditChangeResult
            {
                Succeeded = false,
                Message = message
            };
        }
    }
}
