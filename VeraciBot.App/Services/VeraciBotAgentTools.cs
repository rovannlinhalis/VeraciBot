using System.ComponentModel;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using VeraciBot.App.Data;
using VeraciBot.App.Entities;
using VeraciBot.App.External;

namespace VeraciBot.App.Services
{
    /// <summary>
    /// Tools available to the VeraciBot AI agent. One instance per mention (scoped).
    /// </summary>
    public class VeraciBotAgentTools
    {
        private const int MaxTweetLength = 280;
        private const int MaxNewsResults = 8;
        private const int MaxNewsArticlesToRead = 4;
        private const int MaxTrustedSitesInPriorityQuery = 5;
        private const int MaxArticleTextLength = 1600;
        private const int NewsEvaluationPoints = 1;
        private static readonly Regex HtmlTagRegex = new("<.*?>", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex ScriptStyleRegex = new("<(script|style|noscript|svg|iframe)[^>]*>.*?</\\1>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private static readonly Regex ArticleTagRegex = new("<article\\b[^>]*>(.*?)</article>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private static readonly Regex ParagraphRegex = new("<p\\b[^>]*>(.*?)</p>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private static readonly Regex TitleRegex = new("<title\\b[^>]*>(.*?)</title>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private static readonly Regex MetaDescriptionRegex = new("<meta\\b[^>]*(?:name|property)=[\"'](?:description|og:description|twitter:description)[\"'][^>]*content=[\"'](?<content>.*?)[\"'][^>]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private static readonly Regex HrefRegex = new("<a\\b[^>]*href=[\"'](?<href>[^\"']+)[\"']", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private static readonly Regex WhiteSpaceRegex = new(@"\s+", RegexOptions.Compiled);
        private static readonly Regex UrlRegex = new(@"(?:https?://|www\.)[^\s<>""]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly ApplicationDbContext _db;
        private readonly TwitterAPI _twitterApi;
        private readonly TwitterUserAuthorizationService _twitterAuthorization;
        private readonly ApplicationSettingsService _settings;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<VeraciBotAgentTools> _logger;

        // Context set once per mention via SetContext before agent.RunAsync
        private string _currentTweetId = string.Empty;
        private string _currentAuthorId = string.Empty;
        private string _currentMentionText = string.Empty;
        private string _botUserId = string.Empty;
        private AuthorizedTwitterUser _authorization;
        private TwitterAPI.ThreadContext _fullThread;
        private bool _isSingleTweet;
        private bool _actionExecuted;
        private bool _replied;
        private readonly List<string> _processSteps = [];
        private readonly List<string> _consultedNewsLinks = [];

        public string LastResult { get; private set; } = string.Empty;
        public string LastImagePath { get; private set; } = string.Empty;
        public IReadOnlyList<string> ProcessSteps => _processSteps;
        public IReadOnlyList<string> ConsultedNewsLinks => _consultedNewsLinks;
        public bool HasConsultedNews => _consultedNewsLinks.Count > 0;

        public VeraciBotAgentTools(
            ApplicationDbContext db,
            TwitterAPI twitterApi,
            TwitterUserAuthorizationService twitterAuthorization,
            ApplicationSettingsService settings,
            IHttpClientFactory httpClientFactory,
            ILogger<VeraciBotAgentTools> logger)
        {
            _db = db;
            _twitterApi = twitterApi;
            _twitterAuthorization = twitterAuthorization;
            _settings = settings;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public void SetContext(
            string tweetId,
            string authorId,
            string mentionText,
            string botUserId,
            AuthorizedTwitterUser authorization,
            TwitterAPI.ThreadContext fullThread,
            bool isSingleTweet)
        {
            _currentTweetId = tweetId;
            _currentAuthorId = authorId;
            _currentMentionText = mentionText;
            _botUserId = botUserId;
            _authorization = authorization;
            _fullThread = fullThread;
            _isSingleTweet = isSingleTweet;
            _actionExecuted = false;
            _replied = false;
            LastResult = string.Empty;
            LastImagePath = string.Empty;
            _processSteps.Clear();
            _consultedNewsLinks.Clear();
            AddStep($"Contexto carregado para mencao {_currentTweetId} (autor {_currentAuthorId}).");
        }

        [Description("Gets help context for VeraciBot commands. Use when user asks for help or types !ajuda. Return a final structured response with the text to publish.")]
        public async Task<AgentToolResult> RespondHelp()
        {
            if (!TryBeginAction())
                return AlreadyExecuted();

            LastImagePath = await _settings.GetValueAsync(ApplicationParameter.HELP_IMAGE);

            return CompleteTool(
                "HELP",
                "O usuario pediu ajuda. Gere um texto final curto explicando os comandos disponiveis conforme as orientacoes do system prompt.");
        }

        [Description("Gets the requesting user's current score and stats. Use when user asks for their score or !pontos. Return a final structured response with the text to publish.")]
        public async Task<AgentToolResult> RespondScore()
        {
            if (!TryBeginAction())
                return AlreadyExecuted();

            var user = await _db.AuthorizedTwitterUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.AuthorId == _currentAuthorId);

            var twitterUser = await _twitterApi.GetTwitterUserById(_currentAuthorId);
            var username = twitterUser?.Username ?? _currentAuthorId;

            LastImagePath = await _settings.GetValueAsync(ApplicationParameter.SCORE_IMAGE);

            return CompleteTool(
                "SCORE",
                $"Usuario: @{username}\nScore: {user?.Score ?? 0}\nVitorias: {user?.Wins ?? 0}\nDerrotas: {user?.Losses ?? 0}");
        }

        [Description("Gets the top-10 scoreboard ranking. Use when user asks for the scoreboard or !placar. Return a final structured response with the text to publish.")]
        public async Task<AgentToolResult> RespondScoreboard()
        {
            if (!TryBeginAction())
                return AlreadyExecuted();

            var top10 = await _db.AuthorizedTwitterUsers
                .AsNoTracking()
                .Where(u => u.Status == AuthorizedTwitterUser.STATUS_AUTHORIZED)
                .OrderByDescending(u => u.Score)
                .Take(10)
                .ToListAsync();

            LastImagePath = await _settings.GetValueAsync(ApplicationParameter.SCORE_BOARD_IMAGE);

            if (top10.Count == 0)
                return CompleteTool("SCOREBOARD", "Nao ha usuarios autorizados no placar.");

            var sb = new StringBuilder();
            for (int i = 0; i < top10.Count; i++)
                sb.AppendLine($"{i + 1}. @{top10[i].Username} - {top10[i].Score} pts");

            return CompleteTool("SCOREBOARD", sb.ToString().Trim());
        }

        [Description("Invites another Twitter user to join VeraciBot. Use when the current user wants to invite someone with !convidar @username. Return a final structured response with the text to publish.")]
        public async Task<AgentToolResult> InviteUser(
            [Description("The @username (with or without @) of the Twitter user to invite, as mentioned in the tweet.")] string inviteeUsername)
        {
            if (!TryBeginAction())
                return AlreadyExecuted();

            if (string.IsNullOrWhiteSpace(inviteeUsername))
            {
                LastImagePath = await _settings.GetValueAsync(ApplicationParameter.INVITE_NO_USER_IMAGE);
                return CompleteTool("INVITE_NO_USER", "Nenhum @username foi informado para convite.");
            }

            var twitterUser = await _twitterApi.GetTwitterUserByUserName(inviteeUsername);
            if (twitterUser == null)
            {
                LastImagePath = await _settings.GetValueAsync(ApplicationParameter.INVITE_NO_USER_IMAGE);
                return CompleteTool("INVITE_NO_USER", $"Usuario @{NormalizeUsername(inviteeUsername)} nao encontrado no Twitter.");
            }

            var existingAuth = await _db.AuthorizedTwitterUsers
                .FirstOrDefaultAsync(u => u.AuthorId == twitterUser.Id);

            if (existingAuth != null
                && (existingAuth.Status == AuthorizedTwitterUser.STATUS_AUTHORIZED
                    || existingAuth.Status == AuthorizedTwitterUser.STATUS_INVITED))
            {
                LastImagePath = await _settings.GetValueAsync(ApplicationParameter.INVITE_ERROR_IMAGE);
                return CompleteTool("INVITE_ERROR", $"@{twitterUser.Username} ja esta participando ou aguardando confirmacao de convite.");
            }

            var inviteResult = await RegisterInviteAsync(twitterUser);
            if (!inviteResult.Succeeded)
            {
                LastImagePath = await _settings.GetValueAsync(ApplicationParameter.INVITE_ERROR_IMAGE);
                return CompleteTool("INVITE_ERROR", inviteResult.Message);
            }

            await _twitterAuthorization.SetAuthorizationAsync(
                twitterUser.Id,
                twitterUser.Username,
                twitterUser.Name,
                AuthorizedTwitterUser.STATUS_INVITED,
                changedByAuthorId: _currentAuthorId,
                reason: "INVITE");

            LastImagePath = await _settings.GetValueAsync(ApplicationParameter.INVITE_IMAGE);
            return CompleteTool("INVITE", inviteResult.Message);
        }

        [Description("Accepts the pending invite for the current user. Use when an invited user wants to join VeraciBot with !aceitar. Return a final structured response with the text to publish.")]
        public async Task<AgentToolResult> AcceptInvite()
        {
            if (!TryBeginAction())
                return AlreadyExecuted();

            if (_authorization != null)
            {
                await _twitterAuthorization.SetAuthorizationAsync(
                    _authorization.AuthorId,
                    _authorization.Username,
                    _authorization.Name,
                    AuthorizedTwitterUser.STATUS_AUTHORIZED,
                    changedByAuthorId: _currentAuthorId,
                    reason: "ACCEPT_INVITE");

                await MarkCurrentInviteAsync(TwitterInvite.STATUS_ACCEPTED);
            }

            LastImagePath = await _settings.GetValueAsync(ApplicationParameter.ACCEPT_IMAGE);
            return CompleteTool("ACCEPT", "Convite aceito. O usuario agora esta autorizado a usar o VeraciBot.");
        }

        [Description("Refuses the pending invite for the current user. Use when an invited user wants to decline with !recusar. Return a final structured response with the text to publish.")]
        public async Task<AgentToolResult> RefuseInvite()
        {
            if (!TryBeginAction())
                return AlreadyExecuted();

            if (_authorization != null)
            {
                await _twitterAuthorization.SetAuthorizationAsync(
                    _authorization.AuthorId,
                    _authorization.Username,
                    _authorization.Name,
                    AuthorizedTwitterUser.STATUS_NOT_AUTHORIZED,
                    changedByAuthorId: _currentAuthorId,
                    reason: "REFUSE_INVITE");

                await MarkCurrentInviteAsync(TwitterInvite.STATUS_REFUSED);
            }

            LastImagePath = await _settings.GetValueAsync(ApplicationParameter.REFUSE_IMAGE);
            return CompleteTool("REFUSE", "Convite recusado. O usuario nao ficou autorizado.");
        }

        private async Task<InviteRegistrationResult> RegisterInviteAsync(TwitterAPI.TwitterUser invitee)
        {
            var inviter = await _db.AuthorizedTwitterUsers
                .FirstOrDefaultAsync(x => x.AuthorId == _currentAuthorId);

            if (inviter is null)
                return InviteRegistrationResult.Fail("Nao foi possivel identificar o usuario que esta enviando o convite.");

            var isAdmin = await IsCurrentAuthorAdminAsync();
            if (!isAdmin && inviter.InviteCredits <= 0)
            {
                return InviteRegistrationResult.Fail(
                    "Voce nao tem convites disponiveis. Peca para um administrador adicionar convites ao seu usuario.");
            }

            var now = DateTime.UtcNow;
            if (!isAdmin)
                inviter.InviteCredits--;

            inviter.InvitesSent++;
            inviter.UpdatedAtUtc = now;

            if (string.IsNullOrWhiteSpace(inviter.Username) && !string.IsNullOrWhiteSpace(_authorization?.Username))
                inviter.Username = _authorization.Username;

            _db.TwitterInvites.Add(new TwitterInvite
            {
                InviterAuthorId = _currentAuthorId,
                InviterUsername = inviter.Username,
                InviteeAuthorId = invitee.Id,
                InviteeUsername = NormalizeUsername(invitee.Username),
                InviteeName = invitee.Name ?? string.Empty,
                Status = TwitterInvite.STATUS_PENDING,
                CreatedAtUtc = now,
                SourceTweetId = _currentTweetId
            });

            await _db.SaveChangesAsync();

            var suffix = isAdmin
                ? "Convite administrativo, sem consumir saldo."
                : $"Convites restantes: {inviter.InviteCredits}.";

            return InviteRegistrationResult.Ok(
                $"Convite criado para @{NormalizeUsername(invitee.Username)}. O usuario precisa responder aceitando ou recusando. {suffix}");
        }

        private async Task MarkCurrentInviteAsync(string status)
        {
            if (string.IsNullOrWhiteSpace(_currentAuthorId))
                return;

            var invite = await _db.TwitterInvites
                .Where(x => x.InviteeAuthorId == _currentAuthorId && x.Status == TwitterInvite.STATUS_PENDING)
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync();

            if (invite is null)
                return;

            var now = DateTime.UtcNow;
            invite.Status = status;

            if (status == TwitterInvite.STATUS_ACCEPTED)
            {
                invite.AcceptedAtUtc = now;

                var inviter = await _db.AuthorizedTwitterUsers
                    .FirstOrDefaultAsync(x => x.AuthorId == invite.InviterAuthorId);
                if (inviter is not null)
                {
                    inviter.InvitesAccepted++;
                    inviter.UpdatedAtUtc = now;
                }
            }
            else if (status == TwitterInvite.STATUS_REFUSED)
            {
                invite.RefusedAtUtc = now;
            }

            await _db.SaveChangesAsync();
        }

        private async Task<bool> IsCurrentAuthorAdminAsync()
        {
            if (string.IsNullOrWhiteSpace(_currentAuthorId))
                return false;

            var adminRoleNames = Enum.GetValues<EApplicationRoles>()
                .Where(role => (int)role >= (int)EApplicationRoles.Admin)
                .Select(role => ((int)role).ToString())
                .ToArray();

            return await (
                from user in _db.Users.AsNoTracking()
                join userRole in _db.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
                join role in _db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where user.Enabled &&
                      user.AuthorId == _currentAuthorId &&
                      adminRoleNames.Contains(role.Name)
                select user.Id)
                .AnyAsync();
        }

        [Description("Analyzes the thread argument and determines who is more correct. Only use when isSingleTweet=false and user requests !argumentar. Return a final structured response with the text to publish.")]
        public async Task<AgentToolResult> RespondThreadArgue(
            [Description("Your complete and detailed analysis in Brazilian Portuguese, explaining both sides and concluding who is more correct and why.")] string analysis,
            [Description("Result verdict: 1 if the first person (AuthorA) is more correct, 2 if the second person (AuthorB) is more correct, 0 if it is a draw.")] int result)
        {
            if (!TryBeginAction())
                return AlreadyExecuted();

            var authorAId = _fullThread?.AuthorA ?? string.Empty;
            var authorBId = _fullThread?.AuthorB ?? string.Empty;

            var userA = await _twitterApi.GetTwitterUserById(authorAId);
            var userB = await _twitterApi.GetTwitterUserById(authorBId);

            var authorAUser = await _db.AuthorizedTwitterUsers.FirstOrDefaultAsync(u => u.AuthorId == authorAId);
            var authorBUser = await _db.AuthorizedTwitterUsers.FirstOrDefaultAsync(u => u.AuthorId == authorBId);
            var scoreSettings = await _settings.GetAgentScoreSettingsAsync();

            if (result == 1)
            {
                if (authorAUser != null) { authorAUser.Wins++; authorAUser.Score += scoreSettings.WinPoints; }
                if (authorBUser != null) { authorBUser.Losses++; authorBUser.Score += scoreSettings.LossPoints; }
            }
            else if (result == 2)
            {
                if (authorBUser != null) { authorBUser.Wins++; authorBUser.Score += scoreSettings.WinPoints; }
                if (authorAUser != null) { authorAUser.Losses++; authorAUser.Score += scoreSettings.LossPoints; }
            }
            else
            {
                if (authorAUser != null) { authorAUser.Score += scoreSettings.DrawPoints; }
                if (authorBUser != null) { authorBUser.Score += scoreSettings.DrawPoints; }
            }

            await _db.SaveChangesAsync();

            var scoreDesc = new StringBuilder();
            if (authorAUser != null && userA != null)
                scoreDesc.AppendLine($"@{userA.Username}: {authorAUser.Score} pts");
            if (authorBUser != null && userB != null)
                scoreDesc.AppendLine($"@{userB.Username}: {authorBUser.Score} pts");

            var fullMessage = $"@{userA?.Username ?? authorAId}: {analysis}\n\n{scoreDesc}".Trim();

            var imgParam = result switch
            {
                1 => ApplicationParameter.THREAD_RESULT_A_IMAGE,
                2 => ApplicationParameter.THREAD_RESULT_B_IMAGE,
                _ => ApplicationParameter.THREAD_RESULT_DRAW_IMAGE
            };

            LastImagePath = await _settings.GetValueAsync(imgParam);
            return CompleteTool($"THREAD_ARGUE_{result}", fullMessage);
        }

        [Description("Analyzes whether the information in the thread is true or false. Only use when isSingleTweet=false and user requests !avaliar or !falso. Return a final structured response with the text to publish.")]
        public async Task<AgentToolResult> RespondThreadFalse(
            [Description("Your factual analysis in Brazilian Portuguese on whether the claims in the thread are true, false, or uncertain, with reasoning.")] string analysis,
            [Description("Verdict for the checked claim. Use TRUE when the claim is true, FALSE when it is false, and UNCERTAIN when evidence is insufficient.")] string verdict)
        {
            if (!TryBeginAction())
                return AlreadyExecuted();

            await AwardCurrentAuthorPointsAsync(NewsEvaluationPoints, "NEWS_EVALUATION");
            LastImagePath = await _settings.GetValueAsync(ApplicationParameter.THREAD_RESULT_DRAW_IMAGE);
            return CompleteTool(NormalizeFactCheckResult(verdict), $"Veredito: {verdict}\nAnalise: {analysis}");
        }

        private async Task AwardCurrentAuthorPointsAsync(int points, string reason)
        {
            if (points == 0 || string.IsNullOrWhiteSpace(_currentAuthorId))
                return;

            var now = DateTime.UtcNow;
            var user = await _db.AuthorizedTwitterUsers
                .FirstOrDefaultAsync(x => x.AuthorId == _currentAuthorId);

            if (user is null)
            {
                user = new AuthorizedTwitterUser
                {
                    AuthorId = _currentAuthorId,
                    Username = _authorization?.Username ?? string.Empty,
                    Name = _authorization?.Name ?? string.Empty,
                    Status = _authorization?.Status ?? AuthorizedTwitterUser.STATUS_NOT_AUTHORIZED,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Score = points
                };

                _db.AuthorizedTwitterUsers.Add(user);
                AddStep($"Pontuacao criada para autor {_currentAuthorId}: +{points} ({reason}).");
            }
            else
            {
                user.Score += points;
                user.UpdatedAtUtc = now;
                AddStep($"Pontuacao adicionada para autor {_currentAuthorId}: +{points} ({reason}).");
            }

            await _db.SaveChangesAsync();
        }

        [Description("Analyzes a thread and determines who is right in the debate. Only use when isSingleTweet=false and user requests !quemtemrazao. Return a final structured response with the text to publish.")]
        public async Task<AgentToolResult> RespondThreadWhoIsRight(
            [Description("Your analysis in Brazilian Portuguese explaining who is right in the debate and why, with clear reasoning.")] string analysis)
        {
            if (!TryBeginAction())
                return AlreadyExecuted();

            LastImagePath = await _settings.GetValueAsync(ApplicationParameter.THREAD_RESULT_DRAW_IMAGE);
            return CompleteTool("THREAD_WHOISRIGHT", analysis);
        }

        [Description("Support tool that searches Google News for more information about a reported news claim. Trusted news sites configured by the administrator are queried first as priority, then a broad non-restricted search is also performed. Use this before the final response for current facts, reported news, and claims that need external source checking. This tool does not publish and does not count as the final action tool.")]
        public async Task<AgentToolResult> SearchNewsOnGoogle(
            [Description("A concise Google query describing the reported news or factual claim to investigate.")] string query)
        {
            AddStep("Ferramenta de apoio SearchNewsOnGoogle selecionada para consultar fontes externas.");
            LastImagePath = await _settings.GetValueAsync(ApplicationParameter.THREAD_RESULT_DRAW_IMAGE);

            var searchQuery = BuildGoogleNewsQuery(query);
            if (string.IsNullOrWhiteSpace(searchQuery))
                return CompleteSupportTool("NEWS_SEARCH_EMPTY_QUERY", "Nenhuma noticia ou alegacao foi informada para pesquisa.");

            var trustedSites = ParseTrustedSites(await _settings.GetValueAsync(ApplicationParameter.AGENT_TRUSTED_NEWS_SITES))
                .Take(10)
                .ToArray();
            AddStep($"Pesquisa de noticias iniciada para a consulta: {searchQuery}.");

            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var client = _httpClientFactory.CreateClient();
                var rawResults = new List<NewsSearchItem>();

                var trustedPriorityQuery = BuildTrustedPriorityGoogleNewsQuery(searchQuery, trustedSites);
                if (!string.IsNullOrWhiteSpace(trustedPriorityQuery))
                {
                    var trustedResults = await FetchGoogleNewsResultsAsync(
                        client,
                        trustedPriorityQuery,
                        "prioridade em dominios confiaveis",
                        required: false,
                        cancellationToken: timeout.Token);

                    foreach (var item in trustedResults)
                    {
                        item.IsTrustedPriorityResult = IsTrustedNewsItem(item, trustedSites);
                        rawResults.Add(item);
                    }
                }

                rawResults.AddRange(await FetchGoogleNewsResultsAsync(
                    client,
                    searchQuery,
                    "busca ampla",
                    required: true,
                    cancellationToken: timeout.Token));

                var results = MergeNewsResults(rawResults, trustedSites)
                    .Take(MaxNewsResults)
                    .ToArray();

                var enrichedResults = await EnrichNewsResultsAsync(results);

                foreach (var resultItem in enrichedResults)
                    AddConsultedNewsLink(GetConsultedArticleUrl(resultItem));

                if (enrichedResults.Length == 0)
                {
                    return CompleteSupportTool(
                        "NEWS_SEARCH_EMPTY",
                        trustedSites.Length == 0
                            ? $"Nenhum resultado encontrado no Google News para: {searchQuery}"
                            : $"Nenhum resultado encontrado no Google News para: {searchQuery}\nSites confiaveis configurados: {string.Join(", ", trustedSites)}");
                }

                return CompleteSupportTool("NEWS_SEARCH", FormatNewsSearchResults(trustedSites, enrichedResults));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao pesquisar noticia no Google News.");
                return CompleteSupportTool("NEWS_SEARCH_ERROR", $"Nao foi possivel pesquisar no Google News agora: {ex.Message}");
            }
        }

        [Description("Returns context for an unknown command response. Use when no other command matches or the request is unclear. Return a final structured response with the text to publish.")]
        public async Task<AgentToolResult> RespondUnknownCommand()
        {
            if (!TryBeginAction())
                return AlreadyExecuted();

            LastImagePath = await _settings.GetValueAsync(ApplicationParameter.FAILED_UNDERSTAND_IMAGE);
            return CompleteTool("UNKNOWN", "A mencao nao corresponde a nenhum comando conhecido.");
        }

        public async Task PublishAndMarkAsync(string message, string result, CancellationToken cancellationToken)
        {
            if (_replied)
                return;

            _replied = true;
            LastResult = string.IsNullOrWhiteSpace(result) ? LastResult : result;
            var tweetMessage = NormalizeTweetMessage(message);

            // Mark as processed before posting to prevent retries on post failure
            var existing = await _db.ProcessedMentions
                .FirstOrDefaultAsync(p => p.TweetId == _currentTweetId, cancellationToken);

            if (existing == null)
            {
                _db.ProcessedMentions.Add(new ProcessedMention
                {
                    TweetId = _currentTweetId,
                    MentionUrl = BuildMentionUrl(_currentTweetId),
                    AuthorId = _currentAuthorId,
                    Text = _currentMentionText,
                    ProcessedAtUtc = DateTimeOffset.UtcNow,
                    Result = LastResult
                });
                await _db.SaveChangesAsync(cancellationToken);
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(LastImagePath))
                    await _twitterApi.PostReplyWithImageAsync(tweetMessage, LastImagePath, _currentTweetId);
                else
                    await _twitterApi.PostReplyAsync(tweetMessage, _currentTweetId);

                AddStep("Resposta publicada no Twitter/X.");
            }
            catch (Exception ex)
            {
                AddStep($"Falha ao publicar resposta no Twitter/X: {ex.Message}");
                _logger.LogError(ex, "Erro ao postar resposta para mencao {TweetId}.", _currentTweetId);
                throw;
            }
        }

        private bool TryBeginAction()
        {
            if (_actionExecuted)
                return false;

            _actionExecuted = true;
            AddStep("Ferramenta selecionada para esta mencao.");
            return true;
        }

        private AgentToolResult CompleteTool(string result, string data)
        {
            LastResult = result;
            AddStep($"Ferramenta concluida com resultado {result}.");
            AddConsultedNewsLinksFromText(data);

            return new AgentToolResult
            {
                Success = true,
                Result = result,
                Data = data ?? string.Empty
            };
        }

        private AgentToolResult CompleteSupportTool(string result, string data)
        {
            AddStep($"Ferramenta de apoio concluida com resultado {result}.");

            return new AgentToolResult
            {
                Success = true,
                Result = result,
                Data = data ?? string.Empty
            };
        }

        private AgentToolResult AlreadyExecuted()
        {
            AddStep("Tentativa de executar mais de uma ferramenta na mesma mencao.");
            return new AgentToolResult
            {
                Success = false,
                Result = LastResult,
                Data = "Uma ferramenta ja foi executada nesta mencao. Use o resultado anterior para compor a resposta final."
            };
        }

        private void AddStep(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return;

            _processSteps.Add($"{DateTimeOffset.UtcNow:O} | {description.Trim()}");
        }

        private void AddConsultedNewsLink(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;

            if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
                return;

            if (IsGoogleNewsUrl(uri) || IsLikelyDomainRoot(uri))
                return;

            var normalized = uri.ToString();
            if (_consultedNewsLinks.Contains(normalized, StringComparer.OrdinalIgnoreCase))
                return;

            _consultedNewsLinks.Add(normalized);
            AddStep($"Fonte consultada: {normalized}");
        }

        private void AddConsultedNewsLinksFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            foreach (Match match in UrlRegex.Matches(text))
                AddConsultedNewsLink(match.Value);
        }

        private static string NormalizeFactCheckResult(string verdict)
        {
            if (string.IsNullOrWhiteSpace(verdict))
                return "THREAD_FACT_UNCERTAIN";

            var normalized = verdict.Trim().ToUpperInvariant();

            return normalized switch
            {
                "TRUE" or "VERDADE" or "VERDADEIRO" => "THREAD_FACT_TRUE",
                "FALSE" or "FALSO" or "MENTIRA" => "THREAD_FACT_FALSE",
                _ => "THREAD_FACT_UNCERTAIN"
            };
        }

        private static string NormalizeUsername(string username)
        {
            return username?.Trim().TrimStart('@') ?? string.Empty;
        }

        private static IReadOnlyList<string> ParseTrustedSites(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return [];

            return value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(NormalizeDomain)
                .Where(site => !string.IsNullOrWhiteSpace(site))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static string BuildGoogleNewsQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return string.Empty;

            var cleaned = UrlRegex.Replace(query, " ");
            return WhiteSpaceRegex.Replace(cleaned, " ").Trim();
        }

        private static string BuildTrustedPriorityGoogleNewsQuery(string query, IReadOnlyList<string> trustedSites)
        {
            if (string.IsNullOrWhiteSpace(query) || trustedSites.Count == 0)
                return string.Empty;

            var siteFilters = trustedSites
                .Take(MaxTrustedSitesInPriorityQuery)
                .Select(site => $"site:{site}")
                .ToArray();

            return siteFilters.Length == 0
                ? string.Empty
                : $"{query} ({string.Join(" OR ", siteFilters)})";
        }

        private async Task<IReadOnlyList<NewsSearchItem>> FetchGoogleNewsResultsAsync(
            HttpClient client,
            string query,
            string label,
            bool required,
            CancellationToken cancellationToken)
        {
            var searchUrl = BuildGoogleNewsRssUrl(query);
            AddStep($"Google News RSS consultado ({label}): {searchUrl}");

            using var request = new HttpRequestMessage(HttpMethod.Get, searchUrl);
            request.Headers.UserAgent.ParseAdd("VeraciBot/1.0");
            request.Headers.Accept.ParseAdd("application/rss+xml");

            using var response = await client.SendAsync(request, cancellationToken);
            var rss = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var message = $"Google News retornou HTTP {(int)response.StatusCode} na consulta {label}.";
                if (required)
                    throw new InvalidOperationException(message);

                AddStep($"{message} A busca ampla sera mantida.");
                return [];
            }

            return ParseGoogleNewsRss(rss);
        }

        private static IReadOnlyList<NewsSearchItem> MergeNewsResults(
            IEnumerable<NewsSearchItem> rawResults,
            IReadOnlyList<string> trustedSites)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            return rawResults
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.Title))
                .Where(item => seen.Add(BuildNewsSearchDedupKey(item)))
                .OrderByDescending(item => item.IsTrustedPriorityResult)
                .ThenByDescending(item => IsTrustedNewsItem(item, trustedSites))
                .ToArray();
        }

        private static string BuildNewsSearchDedupKey(NewsSearchItem item)
        {
            var url = Fallback(item.Url, item.ArticleUrl);
            if (!string.IsNullOrWhiteSpace(url))
                return url.Trim();

            return $"{item.SourceName}|{item.Title}".Trim();
        }

        private static string BuildGoogleNewsRssUrl(string query)
        {
            return "https://news.google.com/rss/search?q="
                + Uri.EscapeDataString(query)
                + "&hl=pt-BR&gl=BR&ceid=BR:pt-419";
        }

        private static IReadOnlyList<NewsSearchItem> ParseGoogleNewsRss(string rss)
        {
            var doc = XDocument.Parse(rss);
            var results = doc
                .Descendants("item")
                .Select(ParseGoogleNewsItem)
                .Where(item => !string.IsNullOrWhiteSpace(item.Title))
                .ToArray();

            return results;
        }

        private static NewsSearchItem ParseGoogleNewsItem(XElement item)
        {
            var source = item.Elements().FirstOrDefault(element => element.Name.LocalName == "source");
            var publishedAtRaw = ReadElementValue(item, "pubDate");
            DateTimeOffset.TryParse(publishedAtRaw, out var publishedAt);

            return new NewsSearchItem
            {
                Title = CleanText(ReadElementValue(item, "title")),
                Url = CleanText(ReadElementValue(item, "link")),
                SourceName = CleanText(source?.Value),
                SourceUrl = CleanText(source?.Attribute("url")?.Value),
                PublishedAt = publishedAt == default ? null : publishedAt,
                Summary = StripHtml(ReadElementValue(item, "description"))
            };
        }

        private async Task<NewsSearchItem[]> EnrichNewsResultsAsync(IReadOnlyList<NewsSearchItem> results)
        {
            var enriched = new List<NewsSearchItem>();

            for (var i = 0; i < results.Count; i++)
            {
                var item = results[i];
                if (i < MaxNewsArticlesToRead)
                    item = await TryReadNewsArticleAsync(item);

                enriched.Add(item);
            }

            return enriched.ToArray();
        }

        private async Task<NewsSearchItem> TryReadNewsArticleAsync(NewsSearchItem item)
        {
            if (item is null || string.IsNullOrWhiteSpace(item.Url))
                return item;

            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                var client = _httpClientFactory.CreateClient();
                var articleUrl = await ResolveNewsArticleUrlAsync(client, item, timeout.Token);
                if (string.IsNullOrWhiteSpace(articleUrl))
                {
                    item.FetchStatus = "URL original da materia nao resolvida";
                    return item;
                }

                using var request = new HttpRequestMessage(HttpMethod.Get, articleUrl);
                request.Headers.UserAgent.ParseAdd("VeraciBot/1.0 (+https://veracibot.local)");
                request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");

                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, timeout.Token);
                var resolvedUrl = response.RequestMessage?.RequestUri?.ToString() ?? articleUrl;
                item.ArticleUrl = Fallback(GetUsableArticleUrl(resolvedUrl), articleUrl);

                if (!response.IsSuccessStatusCode)
                {
                    item.FetchStatus = $"HTTP {(int)response.StatusCode}";
                    return item;
                }

                var mediaType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(mediaType) && !mediaType.Contains("html", StringComparison.OrdinalIgnoreCase))
                {
                    item.FetchStatus = $"Conteudo nao HTML ({mediaType})";
                    return item;
                }

                var html = await response.Content.ReadAsStringAsync(timeout.Token);
                item.ArticleTitle = ExtractHtmlTitle(html);
                item.ArticleText = ExtractArticleText(html);
                item.FetchStatus = string.IsNullOrWhiteSpace(item.ArticleText)
                    ? "Conteudo da pagina nao extraido"
                    : "Conteudo da pagina extraido";
            }
            catch (Exception ex)
            {
                item.FetchStatus = $"Falha ao ler pagina: {ex.Message}";
                _logger.LogDebug(ex, "Nao foi possivel ler conteudo da noticia {Url}.", item.Url);
            }

            return item;
        }

        private async Task<string> ResolveNewsArticleUrlAsync(HttpClient client, NewsSearchItem item, CancellationToken cancellationToken)
        {
            var directUrl = GetUsableArticleUrl(item.Url);
            if (!string.IsNullOrWhiteSpace(directUrl))
                return directUrl;

            if (!Uri.TryCreate(item.Url, UriKind.Absolute, out var uri) || !IsGoogleNewsUrl(uri))
                return string.Empty;

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, item.Url);
                request.Headers.UserAgent.ParseAdd("VeraciBot/1.0 (+https://veracibot.local)");
                request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");

                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
                var redirectedUrl = GetUsableArticleUrl(response.RequestMessage?.RequestUri?.ToString() ?? string.Empty);
                if (!string.IsNullOrWhiteSpace(redirectedUrl))
                    return redirectedUrl;

                var mediaType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
                if (!response.IsSuccessStatusCode ||
                    (!string.IsNullOrWhiteSpace(mediaType) && !mediaType.Contains("html", StringComparison.OrdinalIgnoreCase)))
                {
                    return string.Empty;
                }

                var html = await response.Content.ReadAsStringAsync(cancellationToken);
                return ExtractOriginalArticleUrlFromGoogleNewsHtml(html, item.SourceUrl);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Nao foi possivel resolver URL original do Google News {Url}.", item.Url);
                return string.Empty;
            }
        }

        private static string ExtractOriginalArticleUrlFromGoogleNewsHtml(string html, string sourceUrl)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            var preferredDomain = NormalizeDomain(sourceUrl);
            var candidates = new List<string>();
            var decodedHtml = WebUtility.HtmlDecode(html);

            foreach (Match match in HrefRegex.Matches(decodedHtml))
            {
                var candidate = NormalizeArticleCandidateUrl(match.Groups["href"].Value);
                if (!string.IsNullOrWhiteSpace(candidate))
                    candidates.Add(candidate);
            }

            foreach (Match match in UrlRegex.Matches(decodedHtml))
            {
                var candidate = NormalizeArticleCandidateUrl(match.Value);
                if (!string.IsNullOrWhiteSpace(candidate))
                    candidates.Add(candidate);
            }

            candidates = candidates
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!string.IsNullOrWhiteSpace(preferredDomain))
            {
                var preferred = candidates.FirstOrDefault(candidate =>
                {
                    var candidateDomain = NormalizeDomain(candidate);
                    return candidateDomain.Equals(preferredDomain, StringComparison.OrdinalIgnoreCase)
                        || candidateDomain.EndsWith("." + preferredDomain, StringComparison.OrdinalIgnoreCase);
                });

                if (!string.IsNullOrWhiteSpace(preferred))
                    return preferred;
            }

            return candidates.FirstOrDefault() ?? string.Empty;
        }

        private static string NormalizeArticleCandidateUrl(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var candidate = WebUtility.HtmlDecode(value.Trim());
            if (candidate.StartsWith("//", StringComparison.Ordinal))
                candidate = "https:" + candidate;
            else if (candidate.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
                candidate = "https://" + candidate;

            if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
                return string.Empty;

            if (IsGoogleNewsUrl(uri))
            {
                var nested = ExtractUrlFromQuery(uri.Query);
                return string.IsNullOrWhiteSpace(nested)
                    ? string.Empty
                    : NormalizeArticleCandidateUrl(nested);
            }

            return GetUsableArticleUrl(candidate);
        }

        private static string ExtractUrlFromQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return string.Empty;

            foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var pieces = part.Split('=', 2);
                if (pieces.Length != 2)
                    continue;

                var key = Uri.UnescapeDataString(pieces[0]);
                if (!key.Equals("url", StringComparison.OrdinalIgnoreCase) &&
                    !key.Equals("u", StringComparison.OrdinalIgnoreCase) &&
                    !key.Equals("q", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var value = Uri.UnescapeDataString(pieces[1]);
                if (!string.IsNullOrWhiteSpace(GetUsableArticleUrl(value)))
                    return value;
            }

            return string.Empty;
        }

        private static string ReadElementValue(XElement item, string localName)
        {
            return item.Elements().FirstOrDefault(element => element.Name.LocalName == localName)?.Value ?? string.Empty;
        }

        private static string GetConsultedArticleUrl(NewsSearchItem item)
        {
            if (item is null)
                return string.Empty;

            return Fallback(GetUsableArticleUrl(item.ArticleUrl), GetUsableArticleUrl(item.Url));
        }

        private static string GetUsableArticleUrl(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
                return string.Empty;

            if (IsGoogleNewsUrl(uri) || IsLikelyDomainRoot(uri))
                return string.Empty;

            return uri.ToString();
        }

        private static bool IsTrustedNewsItem(NewsSearchItem item, IReadOnlyList<string> trustedSites)
        {
            var host = NormalizeDomain(Fallback(item.ArticleUrl, Fallback(item.SourceUrl, item.Url)));
            if (string.IsNullOrWhiteSpace(host))
                return false;

            return trustedSites.Any(site =>
                host.Equals(site, StringComparison.OrdinalIgnoreCase)
                || host.EndsWith("." + site, StringComparison.OrdinalIgnoreCase));
        }

        private static string FormatNewsSearchResults(IReadOnlyList<string> trustedSites, IReadOnlyList<NewsSearchItem> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Dados de fontes externas para uso interno do agent. Nao copie este bloco literalmente na resposta publica.");

            if (trustedSites.Count > 0)
            {
                sb.AppendLine($"Sites confiaveis configurados: {string.Join(", ", trustedSites)}");
                sb.AppendLine("Estes sites foram consultados com prioridade, mas a busca ampla tambem foi executada sem restringir os resultados a eles.");
            }

            sb.AppendLine("Noticias encontradas e conteudo lido:");

            for (var i = 0; i < results.Count; i++)
            {
                var item = results[i];
                sb.AppendLine($"{i + 1}. {item.Title}");
                sb.AppendLine($"   Fonte: {Fallback(item.SourceName, "Fonte nao informada")}");
                if (item.PublishedAt.HasValue)
                    sb.AppendLine($"   Publicado: {item.PublishedAt.Value.ToUniversalTime():yyyy-MM-dd HH:mm} UTC");

                var articleUrl = GetConsultedArticleUrl(item);
                if (!string.IsNullOrWhiteSpace(articleUrl))
                    sb.AppendLine($"   Link da materia: {articleUrl}");

                if (!string.IsNullOrWhiteSpace(item.FetchStatus))
                    sb.AppendLine($"   Leitura da pagina: {item.FetchStatus}");

                if (!string.IsNullOrWhiteSpace(item.Summary))
                    sb.AppendLine($"   Resumo: {item.Summary}");

                if (!string.IsNullOrWhiteSpace(item.ArticleTitle)
                    && !item.ArticleTitle.Equals(item.Title, StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"   Titulo da pagina: {item.ArticleTitle}");
                }

                if (!string.IsNullOrWhiteSpace(item.ArticleText))
                    sb.AppendLine($"   Conteudo extraido: {item.ArticleText}");
            }

            return sb.ToString().Trim();
        }

        private static string NormalizeDomain(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var normalized = value.Trim();
            if (!normalized.Contains("://", StringComparison.Ordinal))
                normalized = "https://" + normalized;

            if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
                return string.Empty;

            var host = uri.Host.Trim().TrimEnd('.').ToLowerInvariant();
            return host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
                ? host[4..]
                : host;
        }

        private static string CleanText(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : WhiteSpaceRegex.Replace(WebUtility.HtmlDecode(value).Trim(), " ");
        }

        private static string ExtractHtmlTitle(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            var match = TitleRegex.Match(html);
            return match.Success ? StripHtml(match.Groups[1].Value) : string.Empty;
        }

        private static string ExtractArticleText(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            var cleanedHtml = ScriptStyleRegex.Replace(html, " ");
            var articleMatch = ArticleTagRegex.Match(cleanedHtml);
            var contentHtml = articleMatch.Success ? articleMatch.Groups[1].Value : cleanedHtml;

            var parts = new List<string>();
            var metaDescription = ExtractMetaDescription(html);
            if (!string.IsNullOrWhiteSpace(metaDescription))
                parts.Add(metaDescription);

            foreach (Match paragraphMatch in ParagraphRegex.Matches(contentHtml))
            {
                var paragraph = StripHtml(paragraphMatch.Groups[1].Value);
                if (paragraph.Length < 40)
                    continue;

                if (parts.Contains(paragraph, StringComparer.OrdinalIgnoreCase))
                    continue;

                parts.Add(paragraph);
                if (parts.Sum(x => x.Length) >= MaxArticleTextLength)
                    break;
            }

            if (parts.Count == 0)
            {
                var bodyText = StripHtml(contentHtml);
                if (!string.IsNullOrWhiteSpace(bodyText))
                    parts.Add(bodyText);
            }

            return TruncateText(string.Join(" ", parts), MaxArticleTextLength);
        }

        private static string ExtractMetaDescription(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            var match = MetaDescriptionRegex.Match(html);
            return match.Success ? CleanText(match.Groups["content"].Value) : string.Empty;
        }

        private static string StripHtml(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var withoutTags = HtmlTagRegex.Replace(value, " ");
            return CleanText(withoutTags);
        }

        private static string Fallback(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        private static string NormalizeTweetMessage(string message)
        {
            var normalized = WhiteSpaceRegex.Replace(message ?? string.Empty, " ").Trim();

            if (normalized.Length <= MaxTweetLength)
                return normalized;

            return normalized[..(MaxTweetLength - 3)].TrimEnd() + "...";
        }

        private static string TruncateText(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var normalized = WhiteSpaceRegex.Replace(value.Trim(), " ");
            if (normalized.Length <= maxLength)
                return normalized;

            return normalized[..Math.Max(0, maxLength - 3)].TrimEnd() + "...";
        }

        private static bool IsGoogleNewsUrl(Uri uri)
        {
            return uri.Host.Equals("news.google.com", StringComparison.OrdinalIgnoreCase)
                || uri.Host.EndsWith(".news.google.com", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLikelyDomainRoot(Uri uri)
        {
            var path = uri.AbsolutePath?.Trim() ?? string.Empty;
            return (path.Length == 0 || path.Equals("/", StringComparison.Ordinal))
                && string.IsNullOrWhiteSpace(uri.Query);
        }

        private static string BuildMentionUrl(string tweetId)
        {
            if (string.IsNullOrWhiteSpace(tweetId))
                return string.Empty;

            return $"https://x.com/i/web/status/{tweetId.Trim()}";
        }

        public sealed class AgentToolResult
        {
            public bool Success { get; set; }
            public string Result { get; set; } = string.Empty;
            public string Data { get; set; } = string.Empty;
        }

        private sealed class InviteRegistrationResult
        {
            public bool Succeeded { get; set; }
            public string Message { get; set; } = string.Empty;

            public static InviteRegistrationResult Ok(string message)
            {
                return new InviteRegistrationResult
                {
                    Succeeded = true,
                    Message = message
                };
            }

            public static InviteRegistrationResult Fail(string message)
            {
                return new InviteRegistrationResult
                {
                    Succeeded = false,
                    Message = message
                };
            }
        }

        private sealed class NewsSearchItem
        {
            public string Title { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
            public string SourceName { get; set; } = string.Empty;
            public string SourceUrl { get; set; } = string.Empty;
            public string ArticleUrl { get; set; } = string.Empty;
            public string ArticleTitle { get; set; } = string.Empty;
            public string ArticleText { get; set; } = string.Empty;
            public string FetchStatus { get; set; } = string.Empty;
            public DateTimeOffset? PublishedAt { get; set; }
            public string Summary { get; set; } = string.Empty;
            public bool IsTrustedPriorityResult { get; set; }
        }
    }
}
