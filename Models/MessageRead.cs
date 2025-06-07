using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace lol.Models
{
    public class MessageRead
    {
        public int Id { get; set; }
        public int MessageId { get; set; }
        [ForeignKey("MessageId")]
        public Message Message { get; set; }
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }
        public DateTime ReadAt { get; set; } = DateTime.UtcNow;
    }
} 