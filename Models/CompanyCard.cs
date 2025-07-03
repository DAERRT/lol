using System.ComponentModel.DataAnnotations;

namespace lol.Models
{
    public class CompanyCard
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Введите название компании")]
        [Display(Name = "Название компании")]
        [StringLength(200, ErrorMessage = "Название не должно превышать 200 символов")]
        public string CompanyName { get; set; }

        [Required(ErrorMessage = "Опишите компанию")]
        [Display(Name = "Описание компании")]
        [StringLength(1000, ErrorMessage = "Описание не должно превышать 1000 символов")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Укажите контактные данные")]
        [Display(Name = "Контактные данные")]
        [StringLength(500, ErrorMessage = "Контактные данные не должны превышать 500 символов")]
        public string ContactDetails { get; set; }

        [Display(Name = "Документы, подтверждающие принадлежность")]
        public string DocumentPath { get; set; }

        [Display(Name = "Статус модерации")]
        public CompanyCardStatus Status { get; set; } = CompanyCardStatus.Pending;

        [Display(Name = "Комментарий модератора")]
        [StringLength(1000, ErrorMessage = "Комментарий не должен превышать 1000 символов")]
        public string ModeratorComment { get; set; }

        [Display(Name = "Дата создания")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Дата обновления")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
    }

    public enum CompanyCardStatus
    {
        [Display(Name = "На рассмотрении")]
        Pending,
        [Display(Name = "Одобрено")]
        Approved,
        [Display(Name = "Отклонено")]
        Rejected
    }
}
