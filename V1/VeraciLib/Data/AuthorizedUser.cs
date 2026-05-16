using System.ComponentModel.DataAnnotations;
using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using static System.Net.Mime.MediaTypeNames;

namespace VeraciBot.Data
{

    public class AuthorizedUser
    {

        /// <summary>
        /// Id autorizado do usuário
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Quem autorizou o usuário
        /// </summary>
        public string AuthorizedById { get; set; } = string.Empty;

        /// <summary>
        /// Quando o usuário foi autorizado (UTC)
        /// </summary>
        public DateTime AuthorizationDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Autorizado mesmo?
        /// </summary>
        public int Status { get; set; } = STATUS_AUTHORIZED; // 0 = Não, 1 = Sim, 2 = Convidado

        /// <summary>
        /// Autorização máxima por usuário
        /// </summary>
        static public int MaxAuthorizationsPerUser = 5;

        /// <summary>
        /// Número de autorizações disponíveis   
        /// </summary>
        public int NumberOfAuthorizations { get; set; } = MaxAuthorizationsPerUser;

        // Situação
        public const int STATUS_NOT_AUTHORIZED = 0;
        public const int STATUS_AUTHORIZED = 1;
        public const int STATUS_INVITED = 2;

    }

}
