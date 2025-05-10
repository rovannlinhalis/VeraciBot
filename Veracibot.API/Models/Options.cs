namespace Veracibot.API.Models
{
    public class TweeterOptions
    {
        public static string SectionName { get => nameof(TweeterOptions); }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; } 
        public string AccessToken { get; set; } 
        public string AccessSecret { get; set; } 
        public string BearerToken { get; set; }
        public string UserId { get; set; } 
        public string UserName { get; set; }

        public string RestrictUserId { get; set; }
    }
    public class OpenAIOptions
    {
        public static string SectionName { get => nameof(OpenAIOptions); }
        public string ApiKey { get; set; }
        public string SystemPrompt { get; set; }
        public string Model { get; set; }
        public float Temperature { get; set; }
    }

    public class VeracibotOptions
    {
        public static string SectionName { get => nameof(VeracibotOptions); }

        public double InitialBalance { get; set; } = 100.0;

        public int VeracibotTweetIntervalSeconds { get; set; } = 10000;
        public int VeracibotOpenAIIntervalSeconds { get; set; } = 10000;
        public int VeracibotBalanceIntervalSeconds { get; set; } = 10000;

        public Dictionary<ETweetVeracity, ScoreConfiguration> ScoreConfigurations { get; set; } = new()
        {
            { ETweetVeracity.Verdadeiro, new ScoreConfiguration { AuthorChange = 4, OriginalAuthorChange = -5 } },
            { ETweetVeracity.ParcialmenteVerdadeiro, new ScoreConfiguration { AuthorChange = 1, OriginalAuthorChange = -2 } },
            { ETweetVeracity.Neutro, new ScoreConfiguration { AuthorChange = -1, OriginalAuthorChange = 0 } },
            { ETweetVeracity.ParcialmenteFalso, new ScoreConfiguration { AuthorChange = -3, OriginalAuthorChange = 2 } },
            { ETweetVeracity.Falso, new ScoreConfiguration { AuthorChange = -6, OriginalAuthorChange = 5 } },
        };

        //Verdadeiro = 1,
        //ParcialmenteVerdadeiro = 2,
        //Neutro = 3,
        //ParcialmenteFalso = 4,
        //Falso = 5


        /*
             switch (Result)
            {

                case 1:
                    author.Value += 4;
                    originalAuthor.Value -= 5;
                    break;

                case 2:
                    author.Value += 1;
                    originalAuthor.Value -= 2;
                    break;

                case 3:
                    author.Value -= 1;                    
                    break;

                case 4:
                    author.Value -= 3;
                    originalAuthor.Value += 2;
                    break;

                case 5:
                    author.Value -= 6;
                    originalAuthor.Value += 5;
                    break;

            }
         
         */

    }
    public class ScoreConfiguration
    {
        public int AuthorChange { get; set; }
        public int OriginalAuthorChange { get; set; }
    }
}
