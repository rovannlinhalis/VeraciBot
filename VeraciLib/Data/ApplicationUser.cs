using Microsoft.AspNetCore.Identity;

namespace VeraciBot.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {

        public string AuthorId { get; set; } = string.Empty;    

    }

}
