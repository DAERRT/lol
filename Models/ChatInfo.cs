using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace lol.Models
{
    public class ChatInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsGroup { get; set; }
        public bool IsTeamChat { get; set; }
        public List<ParticipantInfo> Participants { get; set; }
    }

    public class ParticipantInfo
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
} 