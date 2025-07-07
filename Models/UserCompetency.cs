using System.ComponentModel.DataAnnotations.Schema;

namespace lol.Models
{
    public class UserCompetency
    {
        public int CompetencyId { get; set; }
        [ForeignKey("CompetencyId")]
        public Competency Competency { get; set; }

        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }
    }
}
