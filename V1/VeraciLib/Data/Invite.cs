using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using static System.Net.Mime.MediaTypeNames;

namespace VeraciBot.Data
{

    public class Invite
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



    }

}
