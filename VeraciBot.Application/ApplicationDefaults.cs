namespace VeraciBot.Application
{
    public static class ApplicationDefaults
    {
        public const int TwitterWorkerPollIntervalSeconds = 60;
        public const int TwitterWorkerInitialLookbackMinutes = 5;
        public const int TwitterWorkerMaxResults = 25;
        public const int TwitterWorkerCursorAdvanceSeconds = 1;
        public const int TwitterWorkerEmptyLookbackSeconds = 5;
        public const int TwitterWorkerMaxQueueSize = 2000;
        public const int TwitterWorkerMaxLogEntries = 500;
        public const int AgentProcessorIdleDelaySeconds = 5;
        public const float OpenAiTemperature = 0.2f;
        public const int OpenAiMaxOutputTokens = 1200;
        public const int AgentScoreWinPoints = 10;
        public const int AgentScoreLossPoints = 0;
        public const int AgentScoreDrawPoints = 0;
        public const string TwitterOAuth2Scopes = "tweet.read tweet.write users.read offline.access media.write";
        public const string AgentTrustedNewsSites = "https://visaolibertaria.com/site/noticias";
    }
}
