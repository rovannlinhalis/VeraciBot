using static System.Net.Mime.MediaTypeNames;

namespace VeraciBot.Data
{

    public class Tweet
    {

        /// <summary>
        /// Id do tweet que chamou o @veracibot
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Texto verificado
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Author que chamou o veracibot
        /// </summary>
        public string AuthorId { get; set; } = string.Empty;

        /// <summary>
        /// Id do tweet original
        /// </summary>
        public string ThreadId { get; set; } = string.Empty;

        /// <summary>
        /// Author que chamou o veracibot
        /// </summary>
        public string OriginalAuthorId { get; set; } = string.Empty;

        /// <summary>
        /// Texto verificado
        /// </summary>
        public string OriginalText { get; set; } = string.Empty;

        /// <summary>
        /// Resultado do openai
        /// </summary>
        public int Result { get; set; } = 0;

        /// <summary>
        /// Data e hora do registro
        /// </summary>
        public DateTime Date { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Calcula resultado dos autores   
        /// </summary>
        /// <returns></returns>
        public async Task ComputeAuthors(VeraciDbContext dbContext)
        {

            TweetAuthor author = await TweetAuthor.GetTweetAuthor(dbContext, AuthorId);
            TweetAuthor originalAuthor = await TweetAuthor.GetTweetAuthor(dbContext, OriginalAuthorId);

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

            dbContext.TweetAuthors.Update(author);
            dbContext.TweetAuthors.Update(originalAuthor);
            dbContext.SaveChanges();

        }

    }

}
