using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace lol.Models
{
    public class TeamRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        [Required]
        public int TeamId { get; set; }
        [ForeignKey("TeamId")]
        public Team Team { get; set; }

        [Display(Name = "Сообщение")]
        public string Message { get; set; }

        [Display(Name = "Статус заявки")]
        public RequestStatus Status { get; set; }

        [Display(Name = "Дата подачи")]
        public DateTime DateCreated { get; set; }

        [Display(Name = "Дата рассмотрения")]
        public DateTime? DateProcessed { get; set; }
    }

    public enum RequestStatus
    {
        [Display(Name = "На рассмотрении")]
        Pending,
        [Display(Name = "Одобрено")]
        Approved,
        [Display(Name = "Отклонено")]
        Rejected
    }
} 