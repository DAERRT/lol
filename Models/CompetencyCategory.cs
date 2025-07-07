using System.ComponentModel.DataAnnotations;

namespace lol.Models
{
    public class CompetencyCategory
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Введите название категории")]
        [Display(Name = "Название категории")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Укажите цвет категории")]
        [Display(Name = "Цвет категории")]
        public string Color { get; set; }
    }
}
