using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Veracibot.API.Models
{
    public class Tweets
    {
        /// <summary>
        /// Id do tweet que chamou o @veracibot
        /// </summary>
        [Key]
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
        /// Data e hora do registro
        /// </summary>
        public DateTime Date { get; set; } = DateTime.UtcNow;

        public ETweetVeracity TweetVeracity { get; set; } = ETweetVeracity.Waiting;
    }

    public class TweetAuthor
    {
        [Key]
        public string Id { get; set; }
        /// <summary>
        /// Tag do usuário (@)
        /// </summary>
        public string UserName { get; set; } 
        /// <summary>
        /// Nome do usuário
        /// </summary>
        public string Name { get; set; } 

        
    }

    public class AuthorBalance
    {
        [Key]
        public long Id { get; set; }
        public DateTime Date { get; set; }
        public string AuthorId { get; set; }
        public string TweetId { get; set; }
        public double Value { get; set; }
        public double PreviousBalance { get; set; }
        public double CurrentBalance { get; set; }
        public BalanceType Type { get; set; } = BalanceType.Initial;
    }

    public enum BalanceType
    {
        Initial = 0,
        BotCall = 100,
        TweetCheck = 200
    }
}
