using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using static System.Net.Mime.MediaTypeNames;

namespace VeraciBot.Data
{

    public class TweetAuthor
    {

        /// <summary>
        /// Id do tweet que chamou o @veracibot
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Tag do usuário (@)
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Nome do usuário
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Resultado do openai
        /// </summary>
        public int Value { get; set; } = 100;

        /// <summary>
        /// Retorna o autor do tweet
        /// </summary>
        /// <param name="id"></param>
        public static async Task<TweetAuthor> GetTweetAuthor(VeraciDbContext dbContext, string id, string username = "", string name = "")
        {

            TweetAuthor author = await dbContext.TweetAuthors.FirstOrDefaultAsync(e => e.Id == id);

            if (author == null)
            {
                author = new TweetAuthor()
                {
                    Id = id,
                    UserName = username,
                    Name = name,
                    Value = 100
                };
                dbContext.TweetAuthors.Add(author);
                dbContext.SaveChanges();
            }

            return author;

        }


    }

}
