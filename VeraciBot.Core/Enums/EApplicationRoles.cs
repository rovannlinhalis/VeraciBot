using System.ComponentModel;

namespace VeraciBot.Core.Enums
{
    public enum EApplicationRoles
    {
        [Description("Usuario")]
        User = 1,

        [Description("Administrador")]
        Admin = 9,

        [Description("System")]
        System = 999
    }
}
