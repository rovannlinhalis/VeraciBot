using System.ComponentModel;

namespace VeraciBot.Core.Enums
{
    public enum EFieldType
    {
        [Description("Yes/No")]
        YesNo = 1,

        [Description("Number")]
        Number = 2,

        [Description("Small Text")]
        SmallText = 3,

        [Description("Multiline Text")]
        MultilineText = 4,

        Markdown = 5,
        Options = 6,
        Password = 7,
        URL = 8,
        ImageUrl = 9,
        Tokens = 10,
        Json = 11,
        Color = 12,
        Html = 13,
        Buttons = 14,
        Currency = 15,

        [Description("Icone")]
        Icon = 17,

        [Description("Texto Formatado")]
        FormattedText = 18,

        [Description("Computed Value")]
        Computed = 1098,

        [Description("System Resource")]
        System = 1099
    }
}
