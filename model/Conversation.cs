using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace server.model
{
    public class Conversation
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Type { get; set; } = "direct";
        // direct now, group later

        public string? Name { get; set; }
        // null for direct chat, group name later

        public Guid CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }

}