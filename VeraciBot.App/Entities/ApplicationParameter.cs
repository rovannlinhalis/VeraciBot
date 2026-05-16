namespace VeraciBot.App.Entities
{
    public sealed record ApplicationParameter(string Value)
        // Placeholders para formatação dos textos dinâmicos do agent
        // {0} = username, {1} = score, {2} = wins, {3} = losses, etc.
    {
        #region Prompts Agent
        public static readonly ApplicationParameter AGENT_SYSTEM_IDENTITY_PROMPT = new(nameof(AGENT_SYSTEM_IDENTITY_PROMPT));
        public static readonly ApplicationParameter AGENT_SYSTEM_RESPONSE_RULES_PROMPT = new(nameof(AGENT_SYSTEM_RESPONSE_RULES_PROMPT));
        public static readonly ApplicationParameter AGENT_SYSTEM_INVITED_USER_PROMPT = new(nameof(AGENT_SYSTEM_INVITED_USER_PROMPT));
        public static readonly ApplicationParameter AGENT_SYSTEM_AUTHORIZED_COMMANDS_PROMPT = new(nameof(AGENT_SYSTEM_AUTHORIZED_COMMANDS_PROMPT));
        public static readonly ApplicationParameter AGENT_SYSTEM_THREAD_COMMANDS_PROMPT = new(nameof(AGENT_SYSTEM_THREAD_COMMANDS_PROMPT));
        public static readonly ApplicationParameter AGENT_SYSTEM_SINGLE_TWEET_PROMPT = new(nameof(AGENT_SYSTEM_SINGLE_TWEET_PROMPT));
        public static readonly ApplicationParameter AGENT_SYSTEM_FALLBACK_PROMPT = new(nameof(AGENT_SYSTEM_FALLBACK_PROMPT));
        public static readonly ApplicationParameter AGENT_HELP_TEXT = new(nameof(AGENT_HELP_TEXT));
        public static readonly ApplicationParameter AGENT_NOT_AUTHORIZED_TEXT = new(nameof(AGENT_NOT_AUTHORIZED_TEXT));
        public static readonly ApplicationParameter AGENT_SCORE_TEXT = new(nameof(AGENT_SCORE_TEXT));
        public static readonly ApplicationParameter AGENT_SCOREBOARD_TEXT = new(nameof(AGENT_SCOREBOARD_TEXT));
        public static readonly ApplicationParameter AGENT_INVITE_TEXT = new(nameof(AGENT_INVITE_TEXT));
        public static readonly ApplicationParameter AGENT_INVITE_NO_USER_TEXT = new(nameof(AGENT_INVITE_NO_USER_TEXT));
        public static readonly ApplicationParameter AGENT_INVITE_ERROR_TEXT = new(nameof(AGENT_INVITE_ERROR_TEXT));
        public static readonly ApplicationParameter AGENT_ACCEPT_TEXT = new(nameof(AGENT_ACCEPT_TEXT));
        public static readonly ApplicationParameter AGENT_REFUSE_TEXT = new(nameof(AGENT_REFUSE_TEXT));
        public static readonly ApplicationParameter AGENT_UNKNOWN_COMMAND_TEXT = new(nameof(AGENT_UNKNOWN_COMMAND_TEXT));
        #endregion
    
        #region Post Imagens
        public static readonly ApplicationParameter NO_AUTHORIZED_IMAGE = new(nameof(NO_AUTHORIZED_IMAGE));
        public static readonly ApplicationParameter FAILED_UNDERSTAND_IMAGE = new(nameof(FAILED_UNDERSTAND_IMAGE));
        public static readonly ApplicationParameter FAILED_UNDERSTAND_ACCEPT_IMAGE = new(nameof(FAILED_UNDERSTAND_ACCEPT_IMAGE));
        public static readonly ApplicationParameter HELP_IMAGE = new(nameof(HELP_IMAGE));
        public static readonly ApplicationParameter SCORE_IMAGE = new(nameof(SCORE_IMAGE));
        public static readonly ApplicationParameter SCORE_BOARD_IMAGE = new(nameof(SCORE_BOARD_IMAGE));
        public static readonly ApplicationParameter INVITE_IMAGE = new(nameof(INVITE_IMAGE));
        public static readonly ApplicationParameter INVITE_ERROR_IMAGE = new(nameof(INVITE_ERROR_IMAGE));
        public static readonly ApplicationParameter INVITE_NO_USER_IMAGE = new(nameof(INVITE_NO_USER_IMAGE));
        public static readonly ApplicationParameter ACCEPT_IMAGE = new(nameof(ACCEPT_IMAGE));
        public static readonly ApplicationParameter REFUSE_IMAGE = new(nameof(REFUSE_IMAGE));
        #endregion

        #region Autenticacao
        public static readonly ApplicationParameter AUTH_DISABLE_LOCAL_LOGIN = new(nameof(AUTH_DISABLE_LOCAL_LOGIN));
        #endregion

        #region Email SMTP
        public static readonly ApplicationParameter SMTP_ENABLED = new(nameof(SMTP_ENABLED));
        public static readonly ApplicationParameter SMTP_HOST = new(nameof(SMTP_HOST));
        public static readonly ApplicationParameter SMTP_PORT = new(nameof(SMTP_PORT));
        public static readonly ApplicationParameter SMTP_USERNAME = new(nameof(SMTP_USERNAME));
        public static readonly ApplicationParameter SMTP_PASSWORD = new(nameof(SMTP_PASSWORD));
        public static readonly ApplicationParameter SMTP_FROM_EMAIL = new(nameof(SMTP_FROM_EMAIL));
        public static readonly ApplicationParameter SMTP_FROM_NAME = new(nameof(SMTP_FROM_NAME));
        public static readonly ApplicationParameter SMTP_ENABLE_SSL = new(nameof(SMTP_ENABLE_SSL));
        #endregion

        #region OpenAI / Agent
        public static readonly ApplicationParameter OPENAI_API_KEY = new(nameof(OPENAI_API_KEY));
        public static readonly ApplicationParameter OPENAI_MODEL = new(nameof(OPENAI_MODEL));
        public static readonly ApplicationParameter OPENAI_TEMPERATURE = new(nameof(OPENAI_TEMPERATURE));
        public static readonly ApplicationParameter OPENAI_MAX_OUTPUT_TOKENS = new(nameof(OPENAI_MAX_OUTPUT_TOKENS));
        public static readonly ApplicationParameter AGENT_PROCESSOR_ENABLED = new(nameof(AGENT_PROCESSOR_ENABLED));
        public static readonly ApplicationParameter AGENT_PROCESSOR_IDLE_DELAY_SECONDS = new(nameof(AGENT_PROCESSOR_IDLE_DELAY_SECONDS));
        public static readonly ApplicationParameter AGENT_SCORE_WIN_POINTS = new(nameof(AGENT_SCORE_WIN_POINTS));
        public static readonly ApplicationParameter AGENT_SCORE_LOSS_POINTS = new(nameof(AGENT_SCORE_LOSS_POINTS));
        public static readonly ApplicationParameter AGENT_SCORE_DRAW_POINTS = new(nameof(AGENT_SCORE_DRAW_POINTS));
        public static readonly ApplicationParameter AGENT_TRUSTED_NEWS_SITES = new(nameof(AGENT_TRUSTED_NEWS_SITES));
        public static readonly ApplicationParameter THREAD_RESULT_A_IMAGE = new(nameof(THREAD_RESULT_A_IMAGE));
        public static readonly ApplicationParameter THREAD_RESULT_B_IMAGE = new(nameof(THREAD_RESULT_B_IMAGE));
        public static readonly ApplicationParameter THREAD_RESULT_DRAW_IMAGE = new(nameof(THREAD_RESULT_DRAW_IMAGE));
        #endregion

        #region Integracao Twitter
        public static readonly ApplicationParameter TWITTER_CLIENT_ID = new(nameof(TWITTER_CLIENT_ID));
        public static readonly ApplicationParameter TWITTER_CLIENT_SECRET = new(nameof(TWITTER_CLIENT_SECRET));
        public static readonly ApplicationParameter TWITTER_ACCESS_TOKEN = new(nameof(TWITTER_ACCESS_TOKEN));
        public static readonly ApplicationParameter TWITTER_ACCESS_SECRET = new(nameof(TWITTER_ACCESS_SECRET));
        public static readonly ApplicationParameter TWITTER_REFRESH_TOKEN = new(nameof(TWITTER_REFRESH_TOKEN));
        public static readonly ApplicationParameter TWITTER_BEARER_TOKEN = new(nameof(TWITTER_BEARER_TOKEN));
        public static readonly ApplicationParameter TWITTER_TOKEN_EXPIRES_AT_UTC = new(nameof(TWITTER_TOKEN_EXPIRES_AT_UTC));
        public static readonly ApplicationParameter TWITTER_TOKEN_SCOPE = new(nameof(TWITTER_TOKEN_SCOPE));
        public static readonly ApplicationParameter TWITTER_OAUTH_MODE = new(nameof(TWITTER_OAUTH_MODE));
        public static readonly ApplicationParameter TWITTER_OAUTH2_SCOPES = new(nameof(TWITTER_OAUTH2_SCOPES));
        public static readonly ApplicationParameter TWITTER_USER_ID = new(nameof(TWITTER_USER_ID));
        public static readonly ApplicationParameter TWITTER_BOT_USERNAME = new(nameof(TWITTER_BOT_USERNAME));
        public static readonly ApplicationParameter TWITTER_BOT_NAME = new(nameof(TWITTER_BOT_NAME));
        public static readonly ApplicationParameter TWITTER_BOT_AUTHORIZED_AT_UTC = new(nameof(TWITTER_BOT_AUTHORIZED_AT_UTC));
        public static readonly ApplicationParameter TWITTER_BOT_AUTHORIZED = new(nameof(TWITTER_BOT_AUTHORIZED));
        public static readonly ApplicationParameter TWITTER_BOT_AUTHORIZED_BY_ID = new(nameof(TWITTER_BOT_AUTHORIZED_BY_ID));
        public static readonly ApplicationParameter TWITTER_OAUTH2_STATE = new(nameof(TWITTER_OAUTH2_STATE));
        public static readonly ApplicationParameter TWITTER_OAUTH2_CODE_VERIFIER = new(nameof(TWITTER_OAUTH2_CODE_VERIFIER));
        public static readonly ApplicationParameter TWITTER_OAUTH_REQUEST_TOKEN = new(nameof(TWITTER_OAUTH_REQUEST_TOKEN));
        public static readonly ApplicationParameter TWITTER_OAUTH_REQUEST_SECRET = new(nameof(TWITTER_OAUTH_REQUEST_SECRET));
        public static readonly ApplicationParameter TWITTER_WORKER_ENABLED = new(nameof(TWITTER_WORKER_ENABLED));
        public static readonly ApplicationParameter TWITTER_WORKER_POLL_INTERVAL_SECONDS = new(nameof(TWITTER_WORKER_POLL_INTERVAL_SECONDS));
        public static readonly ApplicationParameter TWITTER_WORKER_INITIAL_LOOKBACK_MINUTES = new(nameof(TWITTER_WORKER_INITIAL_LOOKBACK_MINUTES));
        public static readonly ApplicationParameter TWITTER_WORKER_MAX_RESULTS = new(nameof(TWITTER_WORKER_MAX_RESULTS));
        public static readonly ApplicationParameter TWITTER_WORKER_START_TIME_UTC = new(nameof(TWITTER_WORKER_START_TIME_UTC));
        public static readonly ApplicationParameter TWITTER_WORKER_CURSOR_ADVANCE_SECONDS = new(nameof(TWITTER_WORKER_CURSOR_ADVANCE_SECONDS));
        public static readonly ApplicationParameter TWITTER_WORKER_EMPTY_LOOKBACK_SECONDS = new(nameof(TWITTER_WORKER_EMPTY_LOOKBACK_SECONDS));
        public static readonly ApplicationParameter TWITTER_WORKER_MAX_QUEUE_SIZE = new(nameof(TWITTER_WORKER_MAX_QUEUE_SIZE));
        public static readonly ApplicationParameter TWITTER_WORKER_MAX_LOG_ENTRIES = new(nameof(TWITTER_WORKER_MAX_LOG_ENTRIES));
        #endregion

        #region Landing / Home Publica
        public static readonly ApplicationParameter LANDING_TITLE = new(nameof(LANDING_TITLE));
        public static readonly ApplicationParameter LANDING_SUBTITLE = new(nameof(LANDING_SUBTITLE));
        public static readonly ApplicationParameter LANDING_DESCRIPTION = new(nameof(LANDING_DESCRIPTION));
        public static readonly ApplicationParameter LANDING_HERO_IMAGE = new(nameof(LANDING_HERO_IMAGE));
        public static readonly ApplicationParameter LANDING_FEATURE_IMAGE = new(nameof(LANDING_FEATURE_IMAGE));
        public static readonly ApplicationParameter LANDING_PRIMARY_CTA_TEXT = new(nameof(LANDING_PRIMARY_CTA_TEXT));
        public static readonly ApplicationParameter LANDING_PRIMARY_CTA_URL = new(nameof(LANDING_PRIMARY_CTA_URL));
        public static readonly ApplicationParameter LANDING_SECONDARY_CTA_TEXT = new(nameof(LANDING_SECONDARY_CTA_TEXT));
        public static readonly ApplicationParameter LANDING_SECONDARY_CTA_URL = new(nameof(LANDING_SECONDARY_CTA_URL));
        #endregion


        public override string ToString() => Value;

        public static implicit operator string(ApplicationParameter feat)
            => feat.Value;

        public static ApplicationParameter From(string value)
            => new(value);
    }
}
