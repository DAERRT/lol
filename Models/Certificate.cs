using System.ComponentModel.DataAnnotations;

namespace lol.Models
{
    public class Certificate
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Укажите путь к файлу сертификата")]
        [Display(Name = "Путь к файлу")]
        public string FilePath { get; set; }

        [Display(Name = "Название сертификата")]
        public string Title { get; set; }

        [Display(Name = "Дата загрузки")]
        public DateTime UploadDate { get; set; } = DateTime.Now;

        [Display(Name = "Пользователь")]
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
