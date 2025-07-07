using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace lol.Models
{
    public enum RequestStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public class TeamRequest
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        [Required]
        public int TeamId { get; set; }
        [ForeignKey("TeamId")]
        public Team Team { get; set; }

        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public bool IsApproved { get; set; } = false;
        public DateTime? ApprovalDate { get; set; }

        // Competencies at the time of request
        public string CompetenciesAtRequestJson { get; set; } = string.Empty;

        // Certificates at the time of request
        public string CertificatesAtRequestJson { get; set; } = string.Empty;

        public RequestStatus Status { get; set; } = RequestStatus.Pending;
        public string Message { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
        public DateTime? DateProcessed { get; set; }
    }
}
