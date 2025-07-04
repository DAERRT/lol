using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace lol.Models
{
    public class KanbanTask
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Введите название задачи")]
        [Display(Name = "Название задачи")]
        [StringLength(200, ErrorMessage = "Название не должно превышать 200 символов")]
        public string Title { get; set; }

        [Display(Name = "Описание задачи")]
        [StringLength(1000, ErrorMessage = "Описание не должно превышать 1000 символов")]
        public string Description { get; set; }

        [Display(Name = "Статус")]
        public KanbanTaskStatus Status { get; set; } = KanbanTaskStatus.ToDo;

        [Display(Name = "Приоритет")]
        public KanbanTaskPriority Priority { get; set; } = KanbanTaskPriority.Medium;

        [Display(Name = "Дата создания")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Дедлайн")]
        public DateTime? Deadline { get; set; }

        public int ProjectId { get; set; }
        [ForeignKey("ProjectId")]
        public Project Project { get; set; }

        public int TeamId { get; set; }
        [ForeignKey("TeamId")]
        public Team Team { get; set; }

        public string CreatedById { get; set; }
        [ForeignKey("CreatedById")]
        public ApplicationUser CreatedBy { get; set; }

        public string AssignedToId { get; set; }
        [ForeignKey("AssignedToId")]
        public ApplicationUser AssignedTo { get; set; }
    }

    public enum KanbanTaskStatus
    {
        [Display(Name = "К выполнению")]
        ToDo,
        [Display(Name = "В процессе")]
        InProgress,
        [Display(Name = "Выполнено")]
        Done
    }

    public enum KanbanTaskPriority
    {
        [Display(Name = "Низкий")]
        Low,
        [Display(Name = "Средний")]
        Medium,
        [Display(Name = "Высокий")]
        High
    }
}
