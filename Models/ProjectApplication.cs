using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace lol.Models
{
    public enum ProjectApplicationStatus
    {
        [Display(Name = "На рассмотрении")] Pending,
        [Display(Name = "Одобрено")] Approved,
        [Display(Name = "Отклонено")] Rejected
    }

    public class ProjectApplication
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }
        [ForeignKey("ProjectId")]
        public Project Project { get; set; }

        [Required]
        public int TeamId { get; set; }
        [ForeignKey("TeamId")]
        public Team Team { get; set; }

        [Display(Name = "Статус заявки")]
        public ProjectApplicationStatus Status { get; set; } = ProjectApplicationStatus.Pending;

        [Display(Name = "Комментарий команды")]
        [StringLength(1000, ErrorMessage = "Комментарий не должен превышать 1000 символов")]
        public string? Message { get; set; }

        [Display(Name = "Дата подачи заявки")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
} 