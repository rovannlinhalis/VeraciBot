using System.ComponentModel.DataAnnotations.Schema;

namespace VeraciBot.App.Entities
{
    public class ApplicationSettings : DynamicField
    {
        [NotMapped]
        public ApplicationParameter Parameter  { get => ApplicationParameter.From(this.Id); set => this.Id = value; }
    }
}
