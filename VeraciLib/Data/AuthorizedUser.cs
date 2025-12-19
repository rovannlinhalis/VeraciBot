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
        public int Status { get; set; } = 1; // 1 = Ativo, 0 = Inativo, 2 = Convite enviado

        /// <summary>
        /// Autorização máxima por usuário
        /// </summary>
        static public int MaxAuthorizationsPerUser = 5;

        /// <summary>
        /// Número de autorizações disponíveis   
        /// </summary>
        public int NumberOfAuthorizations { get; set; } = MaxAuthorizationsPerUser;

    }

}
