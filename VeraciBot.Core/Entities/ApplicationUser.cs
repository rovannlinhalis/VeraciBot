using Microsoft.AspNetCore.Identity;
using VeraciBot.Core.Enums;

namespace VeraciBot.Core.Entities
{
    public class ApplicationUser : IdentityUser<long>
    {
        public string AuthorId { get; set; } = string.Empty;
        public string TwitterUsername { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;

        public virtual List<ApplicationUserRoles> UserRoles { get; set; }
    }

    public class ApplicationUserRoles : IdentityUserRole<long>
    {
        public virtual ApplicationRole Role { get; set; }
    }

    public class ApplicationRole : IdentityRole<long>
    {
        private EApplicationRoles role;

        public EApplicationRoles Role
        {
            get => role;
            set
            {
                role = value;
                base.Name = ((int)role).ToString();
            }
        }

        public override string Name
        {
            get => ((int)Role).ToString();
            set => Role = (EApplicationRoles)(int.TryParse(value, out var parsed) ? parsed : 0);
        }
    }
}
