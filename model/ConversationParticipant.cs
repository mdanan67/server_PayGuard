using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace server.model
{
    public class ConversationParticipant
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ConversationId { get; set; }
        public Conversation? Conversation { get; set; }

        public Guid UserId { get; set; }
        public User? User { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;
    }

}