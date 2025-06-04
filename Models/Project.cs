using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace lol.Models
{
    public class Project
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Введите название идеи")]
        [Display(Name = "Название идеи")]
        [StringLength(200, ErrorMessage = "Название не должно превышать 200 символов")]
        public string IdeaName { get; set; }

        [Required(ErrorMessage = "Опишите проблему")]
        [Display(Name = "Проблема идеи")]
        [StringLength(1000, ErrorMessage = "Описание проблемы не должно превышать 1000 символов")]
        public string Problem { get; set; }

        [Required(ErrorMessage = "Опишите решение")]
        [Display(Name = "Решение проблемы")]
        [StringLength(1000, ErrorMessage = "Описание решения не должно превышать 1000 символов")]
        public string Solution { get; set; }

        [Required(ErrorMessage = "Опишите ожидаемый результат")]
        [Display(Name = "Ожидаемый результат")]
        [StringLength(1000, ErrorMessage = "Описание ожидаемого результата не должно превышать 1000 символов")]
        public string ExpectedResult { get; set; }

        [Required(ErrorMessage = "Опишите необходимые ресурсы")]
        [Display(Name = "Необходимые ресурсы")]
        [StringLength(1000, ErrorMessage = "Описание необходимых ресурсов не должно превышать 1000 символов")]
        public string NecessaryResources { get; set; }

        [Required(ErrorMessage = "Укажите стек технологий")]
        [Display(Name = "Стек технологий")]
        [StringLength(200, ErrorMessage = "Стек технологий не должен превышать 200 символов")]
        public string Stack { get; set; }

        [Required(ErrorMessage = "Укажите заказчика")]
        [Display(Name = "Заказчик")]
        [EmailAddress(ErrorMessage = "Некорректный email адрес")]
        public string Customer { get; set; }

        [Required(ErrorMessage = "Укажите инициатора")]
        [Display(Name = "Инициатор")]
        [EmailAddress(ErrorMessage = "Некорректный email адрес")]
        public string Initiator { get; set; }

        [Display(Name = "Статус идеи")]
        public ProjectStatus Status { get; set; }

        [Display(Name = "Дата создания")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Дата редактирования")]
        public DateTime UpdatedAt { get; set; }

        [Display(Name = "Дата публикации")]
        public DateTime? PublishedAt { get; set; }

        [Display(Name = "Комментарий администратора")]
        [StringLength(1000, ErrorMessage = "Комментарий не должен превышать 1000 символов")]
        public string? EditComment { get; set; }

        public ICollection<Team> ExecutorTeams { get; set; } = new List<Team>();
        public ICollection<ProjectExchange> ProjectExchanges { get; set; } = new List<ProjectExchange>();
    }

    public enum ProjectStatus
    {
        [Display(Name = "Новая")] New,
        [Display(Name = "На редактировании")] Editing,
        [Display(Name = "Утверждена")] Approved,
        [Display(Name = "Опубликована")] Published
    }
} 