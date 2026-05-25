using System.ComponentModel.DataAnnotations.Schema;
using VeraciBot.Core.Enums;
using VeraciBot.Core.Shared;

namespace VeraciBot.Core.Entities
{
    public abstract class DynamicField
    {
        protected string _value;

        public string Id { get; set; }

        [Column("value", TypeName = "TEXT")]
        public virtual string Value { get => _value; set => _value = value; }

        public virtual EFieldType Type { get; set; }

        [NotMapped]
        public string TypeDescription => Type.GetEnumDescription();

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
        public int DecimalPlaces { get; set; }

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
}
