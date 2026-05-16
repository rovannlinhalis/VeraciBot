using Microsoft.AspNetCore.Identity;
using System.ComponentModel;

namespace VeraciBot.App.Entities
{
    // Add profile data for application users by adding properties to the ApplicationUser class
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
        public EApplicationRoles Role { get => role; set { role = value; base.Name = ((int)role).ToString(); } }
        public override string Name { get => ((int)Role).ToString(); set => this.Role = (EApplicationRoles)(int.TryParse(value, out int v) ? v : 0); }
    }

    public enum EApplicationRoles
    {
        [Description("Usuário")]
        User = 1,
        [Description("Administrador")]
        Admin = 9,
        [Description("System")]
        System = 999
    }
}
