using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace lol.Models
{
    public class Team
    {
        public Team()
        {
            Members = new List<ApplicationUser>();
            TeamRequests = new List<TeamRequest>();
            DateCreated = DateTime.Now;
            DateUpdated = DateTime.Now;
            IsPrivate = false;
            Status = TeamStatus.LookingForProject;
        }

        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Введите название команды")]
        [Display(Name = "Название команды")]
        public string Name { get; set; }

        [Display(Name = "Описание команды")]
        public string Desc { get; set; }

        // Связь многие-ко-многим с пользователями (участники команды)
        public ICollection<ApplicationUser> Members { get; set; }

        // Создатель команды
        public string CreatorId { get; set; }
        [ForeignKey("CreatorId")]
        public ApplicationUser Creator { get; set; }

        // Тим-лид
        public string LeaderId { get; set; }
        [ForeignKey("LeaderId")]
        public ApplicationUser Leader { get; set; }

        [Display(Name = "Закрытая")]
        public bool IsPrivate { get; set; }

        [Display(Name = "Статус команды")]
        public TeamStatus Status { get; set; }

        [Display(Name = "Дата создания")]
        public DateTime DateCreated { get; set; }

        [Display(Name = "Дата обновления")]
        public DateTime DateUpdated { get; set; }

        public ICollection<TeamRequest> TeamRequests { get; set; }

        public ICollection<Project>? ExecutorProjects { get; set; }
    }

    public enum TeamStatus
    {
        [Display(Name = "В поиске проекта")]
        LookingForProject,
        [Display(Name = "Работает над проектом")]
        WorkingOnProject
    }
} 