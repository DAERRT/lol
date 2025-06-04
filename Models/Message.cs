using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace lol.Models
{
    public class Message
    {
        public int Id { get; set; }

        public int ChatId { get; set; }
        [ForeignKey("ChatId")]
        public Chat Chat { get; set; }

        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        public string Text { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
} 