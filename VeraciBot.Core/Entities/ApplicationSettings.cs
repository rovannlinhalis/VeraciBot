using System.ComponentModel.DataAnnotations.Schema;

namespace VeraciBot.Core.Entities
{
    public class ApplicationSettings : DynamicField
    {
        [NotMapped]
        public ApplicationParameter Parameter
        {
            get => ApplicationParameter.From(Id);
            set => Id = value;
        }
    }
}
