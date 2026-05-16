using Microsoft.EntityFrameworkCore;
using System.Globalization;
using VeraciBot.App.Data;
using VeraciBot.App.Entities;
using VeraciBot.App.Interfaces;

namespace VeraciBot.App.Services
{

    public class ApplicationSettingsService
    {
        public const int DefaultTwitterWorkerPollIntervalSeconds = 60;
        public const int DefaultTwitterWorkerInitialLookbackMinutes = 5;
        public const int DefaultTwitterWorkerMaxResults = 25;
        public const int DefaultTwitterWorkerCursorAdvanceSeconds = 1;
        public const int DefaultTwitterWorkerEmptyLookbackSeconds = 5;
        public const int DefaultTwitterWorkerMaxQueueSize = 2000;
        public const int DefaultTwitterWorkerMaxLogEntries = 500;
        public const int DefaultAgentProcessorIdleDelaySeconds = 5;
        public const float DefaultOpenAiTemperature = 0.2f;
        public const int DefaultOpenAiMaxOutputTokens = 1200;
        public const int DefaultAgentScoreWinPoints = 10;
        public const int DefaultAgentScoreLossPoints = 0;
        public const int DefaultAgentScoreDrawPoints = 0;
        public const string DefaultTwitterOAuth2Scopes = "tweet.read tweet.write users.read offline.access media.write";
        public const string DefaultAgentTrustedNewsSites = "https://visaolibertaria.com/site/noticias";
        public const string DefaultAgentSystemIdentityPrompt = "Voce e VeraciBot, um assistente de verificacao de informacoes no Twitter.";
        public const string DefaultAgentSystemResponseRulesPrompt =
            "Analise a mencao recebida e chame as ferramentas necessarias para responder ao usuario.\n" +
            "SearchNewsOnGoogle e ferramenta de apoio obrigatoria quando houver avaliacao factual ou noticia.\n" +
            "Responda sempre em portugues do Brasil.";
        public const string DefaultAgentSystemInvitedUserPrompt =
            "IMPORTANTE: O usuario foi convidado mas ainda nao confirmou a participacao.\n" +
            "- Se quiser aceitar o convite (!aceitar, sim, quero participar) -> AcceptInvite\n" +
            "- Se quiser recusar o convite (!recusar, nao quero) -> RefuseInvite\n" +
            "- Para qualquer outra coisa -> RespondUnknownCommand";
        public const string DefaultAgentSystemAuthorizedCommandsPrompt =
            "MAPEAMENTO DE COMANDOS:\n" +
            "- !ajuda, ajuda, help -> RespondHelp\n" +
            "- !pontos, minha pontuacao, meu score -> RespondScore\n" +
            "- !placar, tabela, ranking -> RespondScoreboard\n" +
            "- !convidar @usuario -> InviteUser (extrai o @username do texto)\n" +
            "- pedidos de verificacao de noticia, fato atual ou busca de fontes -> SearchNewsOnGoogle como apoio antes da resposta final";
        public const string DefaultAgentSystemThreadCommandsPrompt =
            "- !argumentar, argumente sobre isso -> RespondThreadArgue (gere sua analise e passe como parametro)\n" +
            "- !avaliar, !falso, isso e verdade? -> SearchNewsOnGoogle obrigatoriamente e depois RespondThreadFalse (gere sua analise com base nas fontes e passe verdict como TRUE, FALSE ou UNCERTAIN)\n" +
            "- !quemtemrazao, quem tem razao -> RespondThreadWhoIsRight (gere sua analise e passe como parametro)";
        public const string DefaultAgentSystemSingleTweetPrompt = "- Pedidos de analise de thread nao se aplicam (tweet simples sem debate) -> RespondUnknownCommand";
        public const string DefaultAgentSystemFallbackPrompt = "- Qualquer outra coisa -> RespondUnknownCommand";

        public static List<ApplicationSettings> _defaultSettings = new List<ApplicationSettings>
        {
            new ApplicationSettings() { Parameter = ApplicationParameter.AGENT_SYSTEM_IDENTITY_PROMPT, Name = "Identidade do Bot", Type = EFieldType.MultilineText, Value = DefaultAgentSystemIdentityPrompt, Description = "Define quem o agente é dentro do system prompt.", Group = "Agent", Order = 100, Subgroup = "Prompt do Sistema", Size = EFieldSize.Full, Height = 96 },
            new ApplicationSettings() { Parameter = ApplicationParameter.AGENT_SYSTEM_RESPONSE_RULES_PROMPT, Name = "Regras Gerais de Resposta", Type = EFieldType.MultilineText, Value = DefaultAgentSystemResponseRulesPrompt, Description = "Orientações gerais usadas em todas as menções.", Group = "Agent", Order = 101, Subgroup = "Prompt do Sistema", Size = EFieldSize.Full, Height = 128 },
            new ApplicationSettings() { Parameter = ApplicationParameter.AGENT_SYSTEM_INVITED_USER_PROMPT, Name = "Usuário Convidado", Type = EFieldType.MultilineText, Value = DefaultAgentSystemInvitedUserPrompt, Description = "Instruções usadas quando o usuário ainda precisa aceitar ou recusar o convite.", Group = "Agent", Order = 102, Subgroup = "Prompt do Sistema", Size = EFieldSize.Full, Height = 160 },
            new ApplicationSettings() { Parameter = ApplicationParameter.AGENT_SYSTEM_AUTHORIZED_COMMANDS_PROMPT, Name = "Comandos Autorizados", Type = EFieldType.MultilineText, Value = DefaultAgentSystemAuthorizedCommandsPrompt, Description = "Mapeamento de comandos disponíveis para usuários autorizados.", Group = "Agent", Order = 103, Subgroup = "Prompt do Sistema", Size = EFieldSize.Full, Height = 160 },
            new ApplicationSettings() { Parameter = ApplicationParameter.AGENT_SYSTEM_THREAD_COMMANDS_PROMPT, Name = "Comandos de Thread", Type = EFieldType.MultilineText, Value = DefaultAgentSystemThreadCommandsPrompt, Description = "Mapeamento usado quando a menção está em uma thread de debate.", Group = "Agent", Order = 104, Subgroup = "Prompt do Sistema", Size = EFieldSize.Full, Height = 160 },
            new ApplicationSettings() { Parameter = ApplicationParameter.AGENT_SYSTEM_SINGLE_TWEET_PROMPT, Name = "Tweet Simples", Type = EFieldType.MultilineText, Value = DefaultAgentSystemSingleTweetPrompt, Description = "Orientação usada quando a menção não está em uma thread de debate.", Group = "Agent", Order = 105, Subgroup = "Prompt do Sistema", Size = EFieldSize.Full, Height = 96 },
            new ApplicationSettings() { Parameter = ApplicationParameter.AGENT_SYSTEM_FALLBACK_PROMPT, Name = "Fallback de Comando", Type = EFieldType.MultilineText, Value = DefaultAgentSystemFallbackPrompt, Description = "Orientação final para mensagens que não correspondem a comandos conhecidos.", Group = "Agent", Order = 106, Subgroup = "Prompt do Sistema", Size = EFieldSize.Full, Height = 96 },
            new ApplicationSettings() { Parameter = ApplicationParameter.AGENT_TRUSTED_NEWS_SITES, Name = "Sites Confiáveis para Notícias", Type = EFieldType.Tokens, Value = DefaultAgentTrustedNewsSites, Description = "Domínios confiáveis consultados com prioridade. A busca no Google News continua ampla e não é filtrada apenas por estes sites.", Group = "Agent", Order = 108, Subgroup = "Pesquisa de Notícias", Size = EFieldSize.Full },
            new ApplicationSettings() { Parameter = ApplicationParameter.HELP_IMAGE, Name = "Imagem de Ajuda", Type = EFieldType.ImageUrl, Value = "img/logo.jpg", Description = "Imagem usada na resposta de ajuda.", Group = "Agent", Order = 130, Subgroup = "Ajuda", Size = EFieldSize.Small },
            new ApplicationSettings() { Parameter = ApplicationParameter.NO_AUTHORIZED_IMAGE, Name = "Imagem de Não Autorizado", Type = EFieldType.ImageUrl, Value = "img/no.jpg", Description = "Imagem usada quando o usuário não está autorizado.", Group = "Agent", Order = 140, Subgroup = "Autorização", Size = EFieldSize.Small },
            new ApplicationSettings() { Parameter = ApplicationParameter.SCORE_IMAGE, Name = "Imagem de Pontuação", Type = EFieldType.ImageUrl, Value = "img/logo.jpg", Description = "Imagem usada na resposta de pontuação individual.", Group = "Agent", Order = 150, Subgroup = "Pontuação - Consulta", Size = EFieldSize.Small },
            new ApplicationSettings() { Parameter = ApplicationParameter.SCORE_BOARD_IMAGE, Name = "Imagem do Placar", Type = EFieldType.ImageUrl, Value = "img/logo.jpg", Description = "Imagem usada na resposta do placar.", Group = "Agent", Order = 160, Subgroup = "Pontuação - Placar", Size = EFieldSize.Small },
            new ApplicationSettings() { Parameter = ApplicationParameter.INVITE_IMAGE, Name = "Imagem de Convite", Type = EFieldType.ImageUrl, Value = "img/invite.jpg", Description = "Imagem usada ao enviar convite.", Group = "Agent", Order = 180, Subgroup = "Convite - Envio", Size = EFieldSize.Small },
            new ApplicationSettings() { Parameter = ApplicationParameter.INVITE_NO_USER_IMAGE, Name = "Imagem de Convite sem Usuário", Type = EFieldType.ImageUrl, Value = "img/no.jpg", Description = "Imagem usada quando o usuário do convite não é informado ou não é encontrado.", Group = "Agent", Order = 190, Subgroup = "Convite - Usuário não encontrado", Size = EFieldSize.Small },
            new ApplicationSettings() { Parameter = ApplicationParameter.INVITE_ERROR_IMAGE, Name = "Imagem de Erro no Convite", Type = EFieldType.ImageUrl, Value = "img/no.jpg", Description = "Imagem usada quando houver erro no convite.", Group = "Agent", Order = 200, Subgroup = "Convite - Erro", Size = EFieldSize.Small },
            new ApplicationSettings() { Parameter = ApplicationParameter.ACCEPT_IMAGE, Name = "Imagem de Aceite", Type = EFieldType.ImageUrl, Value = "img/logo.jpg", Description = "Imagem usada quando o convite é aceito.", Group = "Agent", Order = 210, Subgroup = "Convite - Aceite", Size = EFieldSize.Small },
            new ApplicationSettings() { Parameter = ApplicationParameter.FAILED_UNDERSTAND_ACCEPT_IMAGE, Name = "Imagem de Falha no Aceite", Type = EFieldType.ImageUrl, Value = "img/duvida.jpg", Description = "Imagem reservada para falhas de entendimento durante o aceite do convite.", Group = "Agent", Order = 212, Subgroup = "Convite - Falha no Aceite", Size = EFieldSize.Small },
            new ApplicationSettings() { Parameter = ApplicationParameter.REFUSE_IMAGE, Name = "Imagem de Recusa", Type = EFieldType.ImageUrl, Value = "img/no.jpg", Description = "Imagem usada quando o convite é recusado.", Group = "Agent", Order = 220, Subgroup = "Convite - Recusa", Size = EFieldSize.Small },
            new ApplicationSettings() { Parameter = ApplicationParameter.FAILED_UNDERSTAND_IMAGE, Name = "Imagem de Comando Desconhecido", Type = EFieldType.ImageUrl, Value = "img/duvida.jpg", Description = "Imagem usada quando não for possível entender a mensagem.", Group = "Agent", Order = 240, Subgroup = "Comando Desconhecido", Size = EFieldSize.Small },

            new ApplicationSettings() { Parameter = ApplicationParameter.AUTH_DISABLE_LOCAL_LOGIN, Name = "Desabilitar Login Local", Type = EFieldType.YesNo, Value = "0", Description = "Quando habilitado e houver provedor externo configurado, oculta e bloqueia login e cadastro por email/senha.", Group = "Autenticação", Order = 20, Subgroup = "Login", Size = EFieldSize.Small },

            new ApplicationSettings() { Parameter = ApplicationParameter.SMTP_ENABLED, Name = "Habilitar SMTP", Type = EFieldType.YesNo, Value = "0", Description = "Liga/desliga o envio real de e-mail por SMTP.", Group = "SMTP", Order = 30, Subgroup = "Conexão", Size = EFieldSize.Small },
            new ApplicationSettings() { Parameter = ApplicationParameter.SMTP_HOST, Name = "Servidor SMTP", Type = EFieldType.SmallText, Value = "", Description = "Host do servidor SMTP (ex: smtp.gmail.com).", Group = "SMTP", Order = 31, Subgroup = "Conexão", Size = EFieldSize.Medium },
            new ApplicationSettings() { Parameter = ApplicationParameter.SMTP_PORT, Name = "Porta SMTP", Type = EFieldType.Number, Value = "587", Description = "Porta do servidor SMTP (ex: 587, 465).", Group = "SMTP", Order = 32, Subgroup = "Conexão", Size = EFieldSize.Small },
            new ApplicationSettings() { Parameter = ApplicationParameter.SMTP_ENABLE_SSL, Name = "Usar SSL/TLS", Type = EFieldType.YesNo, Value = "1", Description = "Ativa conexão segura SSL/TLS no SMTP.", Group = "SMTP", Order = 33, Subgroup = "Conexão", Size = EFieldSize.Small },
            new ApplicationSettings() { Parameter = ApplicationParameter.SMTP_USERNAME, Name = "Usuário SMTP", Type = EFieldType.SmallText, Value = "", Description = "Usuário da conta SMTP.", Group = "SMTP", Order = 34, Subgroup = "Autenticação", Size = EFieldSize.Medium },
            new ApplicationSettings() { Parameter = ApplicationParameter.SMTP_PASSWORD, Name = "Senha SMTP", Type = EFieldType.Password, Value = "", Description = "Senha ou app password da conta SMTP.", Group = "SMTP", Order = 35, Subgroup = "Autenticação", Size = EFieldSize.Medium },
            new ApplicationSettings() { Parameter = ApplicationParameter.SMTP_FROM_EMAIL, Name = "E-mail Remetente", Type = EFieldType.SmallText, Value = "", Description = "Endereço remetente usado nos e-mails enviados.", Group = "SMTP", Order = 36, Subgroup = "Remetente", Size = EFieldSize.Medium },
            new ApplicationSettings() { Parameter = ApplicationParameter.SMTP_FROM_NAME, Name = "Nome Remetente", Type = EFieldType.SmallText, Value = "VeraciBot", Description = "Nome exibido como remetente.", Group = "SMTP", Order = 37, Subgroup = "Remetente", Size = EFieldSize.Medium },

            new ApplicationSettings() { Parameter = ApplicationParameter.TWITTER_CLIENT_ID, Name = "Twitter OAuth 2.0 Client ID", Type = EFieldType.Password, Value = "", Description = "Client ID da aplicacao no X/Twitter para OAuth 2.0 Authorization Code com PKCE", Group = "Twitter", Order = 50, Subgroup = "Autenticação", Size = EFieldSize.Medium },
            new ApplicationSettings() { Parameter = ApplicationParameter.TWITTER_CLIENT_SECRET, Name = "Twitter OAuth 2.0 Client Secret", Type = EFieldType.Password, Value = "", Description = "Client Secret da aplicacao no X/Twitter para OAuth 2.0. Use uma app Web App, Automated App ou Bot.", Group = "Twitter", Order = 51, Subgroup = "Autenticação", Size = EFieldSize.Medium },
            new ApplicationSettings() { Parameter = ApplicationParameter.TWITTER_OAUTH2_SCOPES, Name = "Twitter OAuth 2.0 Scopes", Type = EFieldType.SmallText, Value = DefaultTwitterOAuth2Scopes, Description = "Scopes solicitados na autenticacao OAuth 2.0 do bot", Group = "Twitter", Order = 52, Subgroup = "Autenticação", Size = EFieldSize.Full },
            new ApplicationSettings() { Parameter = ApplicationParameter.TWITTER_ACCESS_TOKEN, Name = "Twitter OAuth 2.0 Access Token", Type = EFieldType.Password, Value = "", Description = "Access token OAuth 2.0 da conta usada pelo bot", Group = "Twitter", Order = 53, Subgroup = "Bot", Size = EFieldSize.Medium },
            new ApplicationSettings() { Parameter = ApplicationParameter.TWITTER_REFRESH_TOKEN, Name = "Twitter OAuth 2.0 Refresh Token", Type = EFieldType.Password, Value = "", Description = "Refresh token OAuth 2.0 da conta usada pelo bot", Group = "Twitter", Order = 54, Subgroup = "Bot", Size = EFieldSize.Medium },
            new ApplicationSettings() { Parameter = ApplicationParameter.TWITTER_ACCESS_SECRET, Name = "Twitter Access Secret (OAuth 1.0a legado)", Type = EFieldType.Password, Value = "", Description = "Token secret legado. OAuth 2.0 nao usa este campo.", Group = "Twitter", Order = 55, Subgroup = "Bot", Size = EFieldSize.Medium },
            new ApplicationSettings() { Parameter = ApplicationParameter.TWITTER_BEARER_TOKEN, Name = "Twitter App Bearer Token", Type = EFieldType.Password, Value = "", Description = "Bearer token app-only para chamadas publicas da API v2, quando necessario", Group = "Twitter", Order = 56, Subgroup = "API", Size = EFieldSize.Medium },
            new ApplicationSettings() { Parameter = ApplicationParameter.TWITTER_TOKEN_EXPIRES_AT_UTC, Name = "Access Token Expira em UTC", Type = EFieldType.Computed, Value = "", Description = "Data de expiracao do access token OAuth 2.0", Group = "Twitter", Order = 57, Subgroup = "Bot", Size = EFieldSize.Medium },
            new ApplicationSettings() { Parameter = ApplicationParameter.TWITTER_TOKEN_SCOPE, Name = "Scopes Autorizados", Type = EFieldType.Computed, Value = "", Description = "Scopes retornados pelo X/Twitter na autorizacao", Group = "Twitter", Order = 58, Subgroup = "Bot", Size = EFieldSize.Full },
            new ApplicationSettings() { Parameter = ApplicationParameter.TWITTER_OAUTH_MODE, Name = "Modo OAuth", Type = EFieldType.Computed, Value = "", Description = "Modo de autenticacao ativo para a conta bot", Group = "Twitter", Order = 59, Subgroup = "Bot", Size = EFieldSize.Small },
            new ApplicationSettings() { Parameter = ApplicationParameter.TWITTER_USER_ID, Name = "Twitter User ID do Bot", Type = EFieldType.SmallText, Value = "", Description = "ID do usuário do bot para leitura de menções", Group = "Twitter", Order = 60, Subgroup = "Bot", Size = EFieldSize.Medium },
            new ApplicationSettings() { Parameter = ApplicationParameter.TWITTER_BOT_USERNAME, Name = "Twitter Username do Bot", Type = EFieldType.Computed, Value = "", Description = "Username da conta autorizada para o bot", Group = "Twitter", Order = 61, Subgroup = "Bot", Size = EFieldSize.Medium },
            new ApplicationSettings() { Parameter = ApplicationParameter.TWITTER_BOT_NAME, Name = "Twitter Nome do Bot", Type = EFieldType.Computed, Value = "", Description = "Nome da conta autorizada para o bot", Group = "Twitter", Order = 62, Subgroup = "Bot", Size = EFieldSize.Medium },
            new ApplicationSettings() { Parameter = ApplicationParameter.TWITTER_BOT_AUTHORIZED_AT_UTC, Name = "Autorizado em UTC", Type = EFieldType.Computed, Value = "", Description = "Data da ultima autenticacao OAuth da conta bot", Group = "Twitter", Order = 63, Subgroup = "Bot", Size = EFieldSize.Medium },
            new ApplicationSettings() { Parameter = ApplicationParameter.TWITTER_BOT_AUTHORIZED, Name = "Bot Autorizado", Type = EFieldType.Computed, Value = "0", Description = "Indica se a conta bot foi autorizada via OAuth", Group = "Twitter", Order = 64, Subgroup = "Bot", Size = EFieldSize.Small },
            new ApplicationSettings() { Parameter = ApplicationParameter.TWITTER_WORKER_ENABLED, Name = "Habilitar Worker de Menções", Type = EFieldType.YesNo, Value = "1", Description = "Liga/desliga o worker que lê menções", Group = "Twitter", Order = 70, Subgroup = "Worker", Size = EFieldSize.Small },
            new ApplicationSettings() { Parameter = ApplicationParameter.TWITTER_WORKER_POLL_INTERVAL_SECONDS, Name = "Intervalo de Poll (segundos)", Type = EFieldType.Number, Value = DefaultTwitterWorkerPollIntervalSeconds.ToString(), Description = "Intervalo entre ciclos de leitura de menções", Group = "Twitter", Order = 71, Subgroup = "Worker", Size = EFieldSize.Small },
            new ApplicationSettings() { Parameter = ApplicationParameter.TWITTER_WORKER_INITIAL_LOOKBACK_MINUTES, Name = "Lookback Inicial (minutos)", Type = EFieldType.Number, Value = DefaultTwitterWorkerInitialLookbackMinutes.ToString(), Description = "Janela inicial de busca ao iniciar o worker", Group = "Twitter", Order = 72, Subgroup = "Worker", Size = EFieldSize.Small },
            new ApplicationSettings() { Parameter = ApplicationParameter.TWITTER_WORKER_MAX_RESULTS, Name = "Máximo de Menções por Leitura", Type = EFieldType.Number, Value = DefaultTwitterWorkerMaxResults.ToString(), Description = "Limite de itens por chamada na API de menções", Group = "Twitter", Order = 73, Subgroup = "Worker", Size = EFieldSize.Small },
            new ApplicationSettings() { Parameter = ApplicationParameter.TWITTER_WORKER_START_TIME_UTC, Name = "Start Time UTC (opcional)", Type = EFieldType.SmallText, Value = "", Description = "Exemplo: 2026-01-01T00:00:00Z. Se preenchido, substitui o lookback inicial", Group = "Twitter", Order = 74, Subgroup = "Worker", Size = EFieldSize.Medium },
            new ApplicationSettings() { Parameter = ApplicationParameter.TWITTER_WORKER_CURSOR_ADVANCE_SECONDS, Name = "Avanço do Cursor (segundos)", Type = EFieldType.Number, Value = DefaultTwitterWorkerCursorAdvanceSeconds.ToString(), Description = "Segundos adicionados ao tweet mais recente para evitar releitura no próximo ciclo", Group = "Twitter", Order = 75, Subgroup = "Worker", Size = EFieldSize.Small },
            new ApplicationSettings() { Parameter = ApplicationParameter.TWITTER_WORKER_EMPTY_LOOKBACK_SECONDS, Name = "Lookback sem Menções (segundos)", Type = EFieldType.Number, Value = DefaultTwitterWorkerEmptyLookbackSeconds.ToString(), Description = "Quando não houver menções, volta esse número de segundos no cursor para tolerar atrasos da API", Group = "Twitter", Order = 76, Subgroup = "Worker", Size = EFieldSize.Small },
            new ApplicationSettings() { Parameter = ApplicationParameter.TWITTER_WORKER_MAX_QUEUE_SIZE, Name = "Tamanho Máximo da Fila", Type = EFieldType.Number, Value = DefaultTwitterWorkerMaxQueueSize.ToString(), Description = "Quantidade máxima de menções mantidas na fila em memória", Group = "Twitter", Order = 77, Subgroup = "Worker", Size = EFieldSize.Small },
            new ApplicationSettings() { Parameter = ApplicationParameter.TWITTER_WORKER_MAX_LOG_ENTRIES, Name = "Máximo de Logs em Memória", Type = EFieldType.Number, Value = DefaultTwitterWorkerMaxLogEntries.ToString(), Description = "Quantidade máxima de entradas de log mantidas no painel em memória", Group = "Twitter", Order = 78, Subgroup = "Worker", Size = EFieldSize.Small },

            new ApplicationSettings() { Parameter = ApplicationParameter.LANDING_TITLE, Name = "Título da Landing", Type = EFieldType.SmallText, Value = "VeraciBot", Description = "Título principal da página inicial pública.", Group = "Landing", Order = 300, Subgroup = "Conteúdo", Size = EFieldSize.Large },
            new ApplicationSettings() { Parameter = ApplicationParameter.LANDING_SUBTITLE, Name = "Subtítulo da Landing", Type = EFieldType.SmallText, Value = "Verificação de notícias e debates no X/Twitter", Description = "Subtítulo destacado da landing page.", Group = "Landing", Order = 301, Subgroup = "Conteúdo", Size = EFieldSize.Full },
            new ApplicationSettings() { Parameter = ApplicationParameter.LANDING_DESCRIPTION, Name = "Descrição da Landing", Type = EFieldType.MultilineText, Value = "O VeraciBot ajuda a avaliar alegações em threads, buscar fontes confiáveis e organizar pontuação da comunidade.", Description = "Texto descritivo principal da landing page.", Group = "Landing", Order = 302, Subgroup = "Conteúdo", Size = EFieldSize.Full, Height = 120 },
            new ApplicationSettings() { Parameter = ApplicationParameter.LANDING_HERO_IMAGE, Name = "Imagem Hero", Type = EFieldType.ImageUrl, Value = "img/logo.jpg", Description = "Imagem principal de destaque da página inicial.", Group = "Landing", Order = 310, Subgroup = "Visual", Size = EFieldSize.Medium },
            new ApplicationSettings() { Parameter = ApplicationParameter.LANDING_FEATURE_IMAGE, Name = "Imagem de Funcionalidade", Type = EFieldType.ImageUrl, Value = "img/resp0.jpg", Description = "Imagem da seção de funcionalidades da página inicial.", Group = "Landing", Order = 311, Subgroup = "Visual", Size = EFieldSize.Medium },
            new ApplicationSettings() { Parameter = ApplicationParameter.LANDING_PRIMARY_CTA_TEXT, Name = "Texto CTA Primário", Type = EFieldType.SmallText, Value = "Entrar", Description = "Texto do botão principal da landing.", Group = "Landing", Order = 320, Subgroup = "Ações", Size = EFieldSize.Small },
            new ApplicationSettings() { Parameter = ApplicationParameter.LANDING_PRIMARY_CTA_URL, Name = "URL CTA Primário", Type = EFieldType.URL, Value = "/Account/Login", Description = "Destino do botão principal da landing.", Group = "Landing", Order = 321, Subgroup = "Ações", Size = EFieldSize.Medium },
            new ApplicationSettings() { Parameter = ApplicationParameter.LANDING_SECONDARY_CTA_TEXT, Name = "Texto CTA Secundário", Type = EFieldType.SmallText, Value = "Criar conta", Description = "Texto do botão secundário da landing.", Group = "Landing", Order = 322, Subgroup = "Ações", Size = EFieldSize.Small },
            new ApplicationSettings() { Parameter = ApplicationParameter.LANDING_SECONDARY_CTA_URL, Name = "URL CTA Secundário", Type = EFieldType.URL, Value = "/Account/Register", Description = "Destino do botão secundário da landing.", Group = "Landing", Order = 323, Subgroup = "Ações", Size = EFieldSize.Medium },

            new ApplicationSettings() { Parameter = ApplicationParameter.OPENAI_API_KEY, Name = "OpenAI API Key", Type = EFieldType.Password, Value = "", Description = "Chave da API OpenAI para o agente de IA", Group = "OpenAI", Order = 100, Subgroup = "Autenticação", Size = EFieldSize.Medium },
            new ApplicationSettings() { Parameter = ApplicationParameter.OPENAI_MODEL, Name = "Modelo OpenAI", Type = EFieldType.Options, Value = "gpt-4o-mini", Description = "Modelo a usar (ex: gpt-4o-mini, gpt-4o)", Group = "OpenAI", Order = 101, Subgroup = "Geração", Size = EFieldSize.Small, Options= new [] { "gpt-5.5", "gpt-4o", "gpt-5.4-mini" } },
            new ApplicationSettings() { Parameter = ApplicationParameter.OPENAI_TEMPERATURE, Name = "Temperatura", Type = EFieldType.Number, Value = DefaultOpenAiTemperature.ToString(CultureInfo.InvariantCulture), Description = "Temperatura usada na geração da LLM. Para modelos que não suportam esse parâmetro, ele é suprimido automaticamente.", Group = "OpenAI", Order = 102, Subgroup = "Geração", Size = EFieldSize.Small, DecimalPlaces = 2 },
            new ApplicationSettings() { Parameter = ApplicationParameter.OPENAI_MAX_OUTPUT_TOKENS, Name = "Máximo de Tokens de Saída", Type = EFieldType.Number, Value = DefaultOpenAiMaxOutputTokens.ToString(), Description = "Limite máximo de tokens que a LLM pode gerar. Para modelos que não suportam esse parâmetro, ele é suprimido automaticamente.", Group = "OpenAI", Order = 103, Subgroup = "Geração", Size = EFieldSize.Small },

            new ApplicationSettings() { Parameter = ApplicationParameter.AGENT_PROCESSOR_ENABLED, Name = "Habilitar Processador de Menções", Type = EFieldType.YesNo, Value = "1", Description = "Liga/desliga o processamento automático de menções pelo agente", Group = "Agent", Order = 120, Subgroup = "Processador", Size = EFieldSize.Small },
            new ApplicationSettings() { Parameter = ApplicationParameter.AGENT_PROCESSOR_IDLE_DELAY_SECONDS, Name = "Delay sem Itens na Fila (segundos)", Type = EFieldType.Number, Value = DefaultAgentProcessorIdleDelaySeconds.ToString(), Description = "Tempo que o processador aguarda antes de procurar novas menções quando a fila está vazia", Group = "Agent", Order = 121, Subgroup = "Processador", Size = EFieldSize.Small },
            new ApplicationSettings() { Parameter = ApplicationParameter.THREAD_RESULT_A_IMAGE, Name = "Imagem Resultado A", Type = EFieldType.ImageUrl, Value = "img/resp1.jpg", Description = "Imagem quando o usuário A vence a discussão.", Group = "Agent", Order = 230, Subgroup = "Debate - Resultado", Size = EFieldSize.Small },
            new ApplicationSettings() { Parameter = ApplicationParameter.THREAD_RESULT_B_IMAGE, Name = "Imagem Resultado B", Type = EFieldType.ImageUrl, Value = "img/resp2.jpg", Description = "Imagem quando o usuário B vence a discussão.", Group = "Agent", Order = 231, Subgroup = "Debate - Resultado", Size = EFieldSize.Small },
            new ApplicationSettings() { Parameter = ApplicationParameter.THREAD_RESULT_DRAW_IMAGE, Name = "Imagem Empate/Verificação", Type = EFieldType.ImageUrl, Value = "img/resp0.jpg", Description = "Imagem usada quando há empate ou em respostas de verificação factual.", Group = "Agent", Order = 232, Subgroup = "Debate - Resultado", Size = EFieldSize.Small },
            // Respostas dinâmicas do Agent
            new ApplicationSettings() { Parameter = ApplicationParameter.AGENT_HELP_TEXT, Name = "Texto de Ajuda", Type = EFieldType.MultilineText, Value = "Olá! Sou o VeraciBot. Comandos: !ajuda, !pontos, !placar, !convidar @usuario. Em uma thread: !avaliar, !argumentar, !quemtemrazao.", Description = "Texto retornado quando o usuário pede ajuda.", Group = "Agent", Order = 131, Subgroup = "Ajuda", Size = EFieldSize.Large },
            new ApplicationSettings() { Parameter = ApplicationParameter.AGENT_NOT_AUTHORIZED_TEXT, Name = "Texto de Não Autorizado", Type = EFieldType.MultilineText, Value = "Voce nao esta autorizado a usar o VeraciBot. Peca para alguem te convidar com !convidar @seu_usuario.", Description = "Texto retornado quando o usuário não está autorizado.", Group = "Agent", Order = 141, Subgroup = "Autorização", Size = EFieldSize.Large },
            new ApplicationSettings() { Parameter = ApplicationParameter.AGENT_SCORE_TEXT, Name = "Texto de Pontuação", Type = EFieldType.MultilineText, Value = "@{0} Pontuação: {1} pts | Vitórias: {2} | Derrotas: {3}", Description = "Texto retornado na pontuação. Use placeholders: {0}=username, {1}=score, {2}=wins, {3}=losses", Group = "Agent", Order = 151, Subgroup = "Pontuação - Consulta", Size = EFieldSize.Large },
            new ApplicationSettings() { Parameter = ApplicationParameter.AGENT_SCOREBOARD_TEXT, Name = "Linha do Placar", Type = EFieldType.MultilineText, Value = "{0}. @{1} - {2} pts", Description = "Linha usada no placar. Use placeholders: {0}=posição, {1}=username, {2}=score", Group = "Agent", Order = 161, Subgroup = "Pontuação - Placar", Size = EFieldSize.Large },
            new ApplicationSettings() { Parameter = ApplicationParameter.AGENT_SCORE_WIN_POINTS, Name = "Pontos por Vitória", Type = EFieldType.Number, Value = DefaultAgentScoreWinPoints.ToString(), Description = "Pontos adicionados ao vencedor de uma avaliação de debate.", Group = "Agent", Order = 170, Subgroup = "Pontuação - Regras", Size = EFieldSize.Small },
            new ApplicationSettings() { Parameter = ApplicationParameter.AGENT_SCORE_LOSS_POINTS, Name = "Pontos por Derrota", Type = EFieldType.Number, Value = DefaultAgentScoreLossPoints.ToString(), Description = "Pontos adicionados ao perdedor de uma avaliação de debate. Pode ser negativo.", Group = "Agent", Order = 171, Subgroup = "Pontuação - Regras", Size = EFieldSize.Small },
            new ApplicationSettings() { Parameter = ApplicationParameter.AGENT_SCORE_DRAW_POINTS, Name = "Pontos por Empate", Type = EFieldType.Number, Value = DefaultAgentScoreDrawPoints.ToString(), Description = "Pontos adicionados a cada participante quando a avaliação terminar empatada.", Group = "Agent", Order = 172, Subgroup = "Pontuação - Regras", Size = EFieldSize.Small },
            new ApplicationSettings() { Parameter = ApplicationParameter.AGENT_INVITE_TEXT, Name = "Texto de Convite", Type = EFieldType.MultilineText, Value = "@{0} você foi convidado para o VeraciBot! Responda !aceitar para participar ou !recusar para declinar.", Description = "Texto retornado ao convidar. Use {0}=username", Group = "Agent", Order = 181, Subgroup = "Convite - Envio", Size = EFieldSize.Large },
            new ApplicationSettings() { Parameter = ApplicationParameter.AGENT_INVITE_NO_USER_TEXT, Name = "Texto de Convite sem Usuário", Type = EFieldType.MultilineText, Value = "Mencione o usuário que deseja convidar. Ex: !convidar @amigo", Description = "Texto retornado quando não há usuário para convite.", Group = "Agent", Order = 191, Subgroup = "Convite - Usuário não encontrado", Size = EFieldSize.Large },
            new ApplicationSettings() { Parameter = ApplicationParameter.AGENT_INVITE_ERROR_TEXT, Name = "Texto de Erro no Convite", Type = EFieldType.MultilineText, Value = "@{0} já está participando ou aguardando confirmação de convite.", Description = "Texto retornado quando não é possível convidar. Use {0}=username", Group = "Agent", Order = 201, Subgroup = "Convite - Erro", Size = EFieldSize.Large },
            new ApplicationSettings() { Parameter = ApplicationParameter.AGENT_ACCEPT_TEXT, Name = "Texto de Aceite", Type = EFieldType.MultilineText, Value = "Bem-vindo ao VeraciBot! Agora você pode usar todos os comandos.", Description = "Texto retornado ao aceitar convite.", Group = "Agent", Order = 211, Subgroup = "Convite - Aceite", Size = EFieldSize.Large },
            new ApplicationSettings() { Parameter = ApplicationParameter.AGENT_REFUSE_TEXT, Name = "Texto de Recusa", Type = EFieldType.MultilineText, Value = "Convite recusado. Se mudar de ideia, peça para ser convidado novamente.", Description = "Texto retornado ao recusar convite.", Group = "Agent", Order = 221, Subgroup = "Convite - Recusa", Size = EFieldSize.Large },
            new ApplicationSettings() { Parameter = ApplicationParameter.AGENT_UNKNOWN_COMMAND_TEXT, Name = "Texto de Comando Desconhecido", Type = EFieldType.MultilineText, Value = "Não entendi o que você quer dizer. Digite !ajuda para ver os comandos disponíveis.", Description = "Texto retornado quando o comando não é reconhecido.", Group = "Agent", Order = 241, Subgroup = "Comando Desconhecido", Size = EFieldSize.Large },
        };

        private readonly ApplicationDbContext _dbContext;
        private readonly IBlobStorageService _blobStorage;
        public ApplicationSettingsService(ApplicationDbContext dbContext, IBlobStorageService blobStorage)
        {
            _dbContext = dbContext;
            _blobStorage = blobStorage;
        }


        public async Task<IEnumerable<ApplicationSettings>> UpdateAsync(IEnumerable<ApplicationSettings> applicationSettings)
        {
           
                var filteredIds = applicationSettings.Select(x => x.Id);
                var dbSettings = await _dbContext.ApplicationSettings.Where(x => filteredIds.Contains(x.Id)).ToListAsync();
                List<ApplicationSettings> resultList = new List<ApplicationSettings>();
                foreach (var s in applicationSettings)
                {
                    var currentDb = dbSettings.FirstOrDefault(x => x.Id == s.Id);
                    if (currentDb is null)
                    {
                        var entry = await _dbContext.ApplicationSettings.AddAsync(s);
                        resultList.Add(entry.Entity);
                    }
                    else
                    {
                        currentDb.Value = s.Value;
                        currentDb.Type = s.Type;
                        var entry = _dbContext.ApplicationSettings.Update(currentDb);
                        resultList.Add(entry.Entity);
                    }
                }
                await _dbContext.SaveChangesAsync();

            

                return resultList;
            
           
        }

        public async Task<IEnumerable<ApplicationSettings>> GetAllAsync(string group = null, bool byPassRoles = false)
        {
            List<ApplicationSettings> currentSettings = _defaultSettings;
            var filteredIds = currentSettings.Select(x => x.Id);
            var dbSettings = await _dbContext.ApplicationSettings.Where(x => filteredIds.Contains(x.Id)).ToListAsync();
            var data = currentSettings.GroupJoin(dbSettings,
                    cs => cs.Id,
                    dbs => dbs.Id,
                    (cs, dbs) => new { cs, dbs = dbs.FirstOrDefault() })
                .Select(x => new ApplicationSettings
                {
                    Id = x.cs.Id,
                    Name = x.cs.Name,
                    Group = x.cs.Group,
                    Description = x.cs.Description,
                    Type = x.cs.Type,
                    Options = x.cs.Options,
                    Order = x.cs.Order,
                    Value = x.dbs != null ? x.dbs.Value : x.cs.Value,
                    DecimalPlaces = x.cs.DecimalPlaces,
                    Subgroup = x.cs.Subgroup,
                    Height = x.cs.Height,
                    Size = x.cs.Size,
                    PlaceHolder = x.cs.PlaceHolder
                })
                //.Where(x => byPassRoles || (x.Type != EFieldType.System && (int)x.MinApplicationRoleToRead <= (int)currentUserRole))
                .OrderBy(x => x.Order).ToList();

            foreach (var setting in data)
            {
                if (setting.Type == EFieldType.ImageUrl)
                {
                    setting.FileUrl = await _blobStorage.GetPresignedUrl(setting.Value);
                }
            }
            return data;
        }

        public async Task<string> GetValueAsync(ApplicationParameter parameter)
        {
            var defaultSetting = _defaultSettings.FirstOrDefault(x => x.Id == parameter.Value);
            if (defaultSetting is null)
                return null;

            var dbSetting = await _dbContext.ApplicationSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == parameter.Value);

            return dbSetting?.Value ?? defaultSetting.Value;
        }

        public async Task<bool> IsLocalLoginDisabledAsync()
        {
            var value = await GetValueAsync(ApplicationParameter.AUTH_DISABLE_LOCAL_LOGIN);
            return ParseYesNo(value, defaultValue: false);
        }

        public async Task<TwitterMentionsWorkerSettings> GetTwitterMentionsWorkerSettingsAsync()
        {
            var enabledRaw = await GetValueAsync(ApplicationParameter.TWITTER_WORKER_ENABLED);
            var pollIntervalRaw = await GetValueAsync(ApplicationParameter.TWITTER_WORKER_POLL_INTERVAL_SECONDS);
            var lookbackRaw = await GetValueAsync(ApplicationParameter.TWITTER_WORKER_INITIAL_LOOKBACK_MINUTES);
            var maxResultsRaw = await GetValueAsync(ApplicationParameter.TWITTER_WORKER_MAX_RESULTS);
            var startTimeRaw = await GetValueAsync(ApplicationParameter.TWITTER_WORKER_START_TIME_UTC);
            var cursorAdvanceRaw = await GetValueAsync(ApplicationParameter.TWITTER_WORKER_CURSOR_ADVANCE_SECONDS);
            var emptyLookbackRaw = await GetValueAsync(ApplicationParameter.TWITTER_WORKER_EMPTY_LOOKBACK_SECONDS);
            var userId = await GetValueAsync(ApplicationParameter.TWITTER_USER_ID) ?? string.Empty;

            return new TwitterMentionsWorkerSettings
            {
                Enabled = ParseYesNo(enabledRaw, defaultValue: true),
                PollIntervalSeconds = Math.Max(10, ParseInt(pollIntervalRaw, DefaultTwitterWorkerPollIntervalSeconds)),
                InitialLookbackMinutes = Math.Max(1, ParseInt(lookbackRaw, DefaultTwitterWorkerInitialLookbackMinutes)),
                MaxResults = Math.Clamp(ParseInt(maxResultsRaw, DefaultTwitterWorkerMaxResults), 5, 100),
                StartTimeUtc = ParseDateTimeOffset(startTimeRaw),
                CursorAdvanceSeconds = Math.Clamp(ParseInt(cursorAdvanceRaw, DefaultTwitterWorkerCursorAdvanceSeconds), 0, 60),
                EmptyLookbackSeconds = Math.Clamp(ParseInt(emptyLookbackRaw, DefaultTwitterWorkerEmptyLookbackSeconds), 0, 300),
                UserId = userId.Trim()
            };
        }

        public async Task<TwitterMentionsRuntimeSettings> GetTwitterMentionsRuntimeSettingsAsync()
        {
            var maxQueueSizeRaw = await GetValueAsync(ApplicationParameter.TWITTER_WORKER_MAX_QUEUE_SIZE);
            var maxLogEntriesRaw = await GetValueAsync(ApplicationParameter.TWITTER_WORKER_MAX_LOG_ENTRIES);

            return new TwitterMentionsRuntimeSettings
            {
                MaxQueueSize = Math.Clamp(ParseInt(maxQueueSizeRaw, DefaultTwitterWorkerMaxQueueSize), 100, 100000),
                MaxLogEntries = Math.Clamp(ParseInt(maxLogEntriesRaw, DefaultTwitterWorkerMaxLogEntries), 100, 10000)
            };
        }

        private static bool ParseYesNo(string value, bool defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            if (value.Equals("1", StringComparison.OrdinalIgnoreCase) || value.Equals("true", StringComparison.OrdinalIgnoreCase))
                return true;

            if (value.Equals("0", StringComparison.OrdinalIgnoreCase) || value.Equals("false", StringComparison.OrdinalIgnoreCase))
                return false;

            return defaultValue;
        }

        private static int ParseInt(string value, int defaultValue)
        {
            return int.TryParse(value, out var parsed)
                ? parsed
                : defaultValue;
        }

        private static float ParseFloat(string value, float defaultValue)
        {
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var invariantParsed))
                return invariantParsed;

            return float.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out var currentCultureParsed)
                ? currentCultureParsed
                : defaultValue;
        }

        private static DateTimeOffset? ParseDateTimeOffset(string value)
        {
            return DateTimeOffset.TryParse(value, out var parsed)
                ? parsed.ToUniversalTime()
                : null;
        }

        public sealed class TwitterMentionsWorkerSettings
        {
            public bool Enabled { get; set; }
            public int PollIntervalSeconds { get; set; }
            public int InitialLookbackMinutes { get; set; }
            public int MaxResults { get; set; }
            public DateTimeOffset? StartTimeUtc { get; set; }
            public int CursorAdvanceSeconds { get; set; }
            public int EmptyLookbackSeconds { get; set; }
            public string UserId { get; set; } = string.Empty;
        }

        public sealed class TwitterMentionsRuntimeSettings
        {
            public int MaxQueueSize { get; set; } = DefaultTwitterWorkerMaxQueueSize;
            public int MaxLogEntries { get; set; } = DefaultTwitterWorkerMaxLogEntries;
        }

        public async Task<AgentProcessorSettings> GetAgentProcessorSettingsAsync()
        {
            var enabledRaw = await GetValueAsync(ApplicationParameter.AGENT_PROCESSOR_ENABLED);
            var idleDelayRaw = await GetValueAsync(ApplicationParameter.AGENT_PROCESSOR_IDLE_DELAY_SECONDS);
            var apiKey = await GetValueAsync(ApplicationParameter.OPENAI_API_KEY) ?? string.Empty;
            var modelRaw = await GetValueAsync(ApplicationParameter.OPENAI_MODEL);
            var temperatureRaw = await GetValueAsync(ApplicationParameter.OPENAI_TEMPERATURE);
            var maxOutputTokensRaw = await GetValueAsync(ApplicationParameter.OPENAI_MAX_OUTPUT_TOKENS);

            return new AgentProcessorSettings
            {
                Enabled = ParseYesNo(enabledRaw, defaultValue: true),
                IdleDelaySeconds = Math.Clamp(ParseInt(idleDelayRaw, DefaultAgentProcessorIdleDelaySeconds), 1, 300),
                OpenAiApiKey = apiKey.Trim(),
                OpenAiModel = !string.IsNullOrWhiteSpace(modelRaw) ? modelRaw.Trim() : "gpt-4o-mini",
                OpenAiTemperature = Math.Clamp(ParseFloat(temperatureRaw, DefaultOpenAiTemperature), 0f, 2f),
                OpenAiMaxOutputTokens = Math.Clamp(ParseInt(maxOutputTokensRaw, DefaultOpenAiMaxOutputTokens), 128, 32000)
            };
        }

        public async Task<AgentScoreSettings> GetAgentScoreSettingsAsync()
        {
            var winPointsRaw = await GetValueAsync(ApplicationParameter.AGENT_SCORE_WIN_POINTS);
            var lossPointsRaw = await GetValueAsync(ApplicationParameter.AGENT_SCORE_LOSS_POINTS);
            var drawPointsRaw = await GetValueAsync(ApplicationParameter.AGENT_SCORE_DRAW_POINTS);

            return new AgentScoreSettings
            {
                WinPoints = Math.Clamp(ParseInt(winPointsRaw, DefaultAgentScoreWinPoints), -1000, 1000),
                LossPoints = Math.Clamp(ParseInt(lossPointsRaw, DefaultAgentScoreLossPoints), -1000, 1000),
                DrawPoints = Math.Clamp(ParseInt(drawPointsRaw, DefaultAgentScoreDrawPoints), -1000, 1000)
            };
        }

        public async Task<AgentSystemPromptSettings> GetAgentSystemPromptSettingsAsync()
        {
            var promptIds = new[]
            {
                ApplicationParameter.AGENT_SYSTEM_IDENTITY_PROMPT.Value,
                ApplicationParameter.AGENT_SYSTEM_RESPONSE_RULES_PROMPT.Value,
                ApplicationParameter.AGENT_SYSTEM_INVITED_USER_PROMPT.Value,
                ApplicationParameter.AGENT_SYSTEM_AUTHORIZED_COMMANDS_PROMPT.Value,
                ApplicationParameter.AGENT_SYSTEM_THREAD_COMMANDS_PROMPT.Value,
                ApplicationParameter.AGENT_SYSTEM_SINGLE_TWEET_PROMPT.Value,
                ApplicationParameter.AGENT_SYSTEM_FALLBACK_PROMPT.Value
            };

            var dbSettings = await _dbContext.ApplicationSettings
                .AsNoTracking()
                .Where(x => promptIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Value);

            return new AgentSystemPromptSettings
            {
                IdentityPrompt = ReadPromptValue(dbSettings, ApplicationParameter.AGENT_SYSTEM_IDENTITY_PROMPT, DefaultAgentSystemIdentityPrompt),
                ResponseRulesPrompt = ReadPromptValue(dbSettings, ApplicationParameter.AGENT_SYSTEM_RESPONSE_RULES_PROMPT, DefaultAgentSystemResponseRulesPrompt),
                InvitedUserPrompt = ReadPromptValue(dbSettings, ApplicationParameter.AGENT_SYSTEM_INVITED_USER_PROMPT, DefaultAgentSystemInvitedUserPrompt),
                AuthorizedCommandsPrompt = ReadPromptValue(dbSettings, ApplicationParameter.AGENT_SYSTEM_AUTHORIZED_COMMANDS_PROMPT, DefaultAgentSystemAuthorizedCommandsPrompt),
                ThreadCommandsPrompt = ReadPromptValue(dbSettings, ApplicationParameter.AGENT_SYSTEM_THREAD_COMMANDS_PROMPT, DefaultAgentSystemThreadCommandsPrompt),
                SingleTweetPrompt = ReadPromptValue(dbSettings, ApplicationParameter.AGENT_SYSTEM_SINGLE_TWEET_PROMPT, DefaultAgentSystemSingleTweetPrompt),
                FallbackPrompt = ReadPromptValue(dbSettings, ApplicationParameter.AGENT_SYSTEM_FALLBACK_PROMPT, DefaultAgentSystemFallbackPrompt)
            };
        }

        private static string ReadPromptValue(
            IReadOnlyDictionary<string, string> dbSettings,
            ApplicationParameter parameter,
            string defaultValue)
        {
            return dbSettings.TryGetValue(parameter.Value, out var value)
                ? value
                : defaultValue;
        }

        public sealed class AgentProcessorSettings
        {
            public bool Enabled { get; set; }
            public int IdleDelaySeconds { get; set; } = DefaultAgentProcessorIdleDelaySeconds;
            public string OpenAiApiKey { get; set; } = string.Empty;
            public string OpenAiModel { get; set; } = "gpt-4o-mini";
            public float OpenAiTemperature { get; set; } = DefaultOpenAiTemperature;
            public int OpenAiMaxOutputTokens { get; set; } = DefaultOpenAiMaxOutputTokens;
        }

        public sealed class AgentScoreSettings
        {
            public int WinPoints { get; set; } = DefaultAgentScoreWinPoints;
            public int LossPoints { get; set; } = DefaultAgentScoreLossPoints;
            public int DrawPoints { get; set; } = DefaultAgentScoreDrawPoints;
        }

        public sealed class AgentSystemPromptSettings
        {
            public string IdentityPrompt { get; set; } = DefaultAgentSystemIdentityPrompt;
            public string ResponseRulesPrompt { get; set; } = DefaultAgentSystemResponseRulesPrompt;
            public string InvitedUserPrompt { get; set; } = DefaultAgentSystemInvitedUserPrompt;
            public string AuthorizedCommandsPrompt { get; set; } = DefaultAgentSystemAuthorizedCommandsPrompt;
            public string ThreadCommandsPrompt { get; set; } = DefaultAgentSystemThreadCommandsPrompt;
            public string SingleTweetPrompt { get; set; } = DefaultAgentSystemSingleTweetPrompt;
            public string FallbackPrompt { get; set; } = DefaultAgentSystemFallbackPrompt;
        }
    }
}
