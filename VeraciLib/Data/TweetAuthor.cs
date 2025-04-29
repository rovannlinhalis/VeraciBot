namespace VeraciBot.Data
{

    public class TweetAuthor
    {

        /// <summary>
        /// Id do tweet que chamou o @veracibot
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Nome do usuário
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Resultado do openai
        /// </summary>
        public int Value { get; set; } = 100;    


    }

}
