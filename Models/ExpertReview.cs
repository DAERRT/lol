using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace lol.Models
{
    public class ExpertReview
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }
        [ForeignKey("ProjectId")]
        public Project Project { get; set; }

        [Required]
        public string ExpertId { get; set; }
        [ForeignKey("ExpertId")]
        public ApplicationUser Expert { get; set; }

        [Required]
        public bool IsApproved { get; set; }

        [StringLength(1000, ErrorMessage = "Комментарий не должен превышать 1000 символов")]
        public string? Comment { get; set; }

        [Display(Name = "Дата оценки")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
} 