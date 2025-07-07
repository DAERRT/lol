using System.ComponentModel.DataAnnotations;

namespace lol.Models
{
    public class Competency
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Введите название компетенции")]
        [Display(Name = "Название компетенции")]
        public string Name { get; set; }

        [Display(Name = "Категория")]
        public int CategoryId { get; set; }
        public CompetencyCategory Category { get; set; }

        public ICollection<ApplicationUser> Users { get; set; }
    }
}
