using System.ComponentModel.DataAnnotations.Schema;

namespace lol.Models
{
    public class MessageAttachment
    {
        public int Id { get; set; }
        public int MessageId { get; set; }
        [ForeignKey("MessageId")]
        public Message Message { get; set; }
        public string? FileName { get; set; }
        public string? FilePath { get; set; }
        public long FileSize { get; set; }
        public string? ContentType { get; set; }
    }
} 