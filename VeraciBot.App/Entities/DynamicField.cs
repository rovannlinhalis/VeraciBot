using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using VeraciBot.App.Shared;

namespace VeraciBot.App.Entities
{
    public abstract class DynamicField
    {
        protected string _value;

        public string Id { get; set; }
        [Column("value", TypeName = "TEXT")]
        public virtual string Value { get => _value; set => _value = value; }

        public virtual EFieldType Type { get; set; }

        [NotMapped]
        public string TypeDescription { get => Type.GetEnumDescription(); }
        [NotMapped]
        public string Name { get; set; }
        [NotMapped]
        public string Description { get; set; }
        [NotMapped]
        public string Group { get; set; }
        [NotMapped]
        public string Subgroup { get; set; }
        [NotMapped]
        public int Order { get; set; }
        [NotMapped]
        public string[] Options { get; set; }
        [NotMapped]
        public int DecimalPlaces { get; set; } = 0;
        [NotMapped]
        public string PlaceHolder { get; set; }

        [NotMapped]
        public EFieldSize Size { get; set; } = EFieldSize.Medium;

        [NotMapped]
        public int? Height { get; set; }

        [NotMapped]
        public bool BoolValue
        {
            get
            {
                if (Value == "1" || Value?.ToLower() == "true")
                    return true;

                return false;
            }
            set
            {
                if (Type == EFieldType.YesNo)
                    Value = value ? "1" : "0";
            }
        }

        [NotMapped]
        public string FileUrl { get; set; }
    }

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
        [Description("Ícone")]
        Icon = 17,
        [Description("Texto Formatado")]
        FormattedText = 18,

        [Description("Computed Value")]
        Computed = 1098,
        [Description("System Resource")]
        System = 1099
    }

    public enum EFieldSize
    {
        [Description("Pequeno")]
        Small = 0,

        [Description("Médio")]
        Medium = 1,

        [Description("Grande")]
        Large = 2,

        [Description("Largura total")]
        Full = 3,
    }
}
