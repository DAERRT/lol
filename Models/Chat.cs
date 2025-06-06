using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace lol.Models
{
    public class Chat
    {
        public int Id { get; set; }

        [MaxLength(100)]
        public string? Name { get; set; } // Для групп/команд

        public bool IsGroup { get; set; } // true - групповой чат, false - личный
        public bool IsTeamChat { get; set; } // true - чат команды

        [MaxLength(200)]
        public string? AvatarPath { get; set; } = "/images/avatars/group.png";

        public ICollection<ChatUser> ChatUsers { get; set; } = new List<ChatUser>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
} 