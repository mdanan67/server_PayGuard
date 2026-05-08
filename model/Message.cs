using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace server.model
{
    public class Message
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ConversationId { get; set; }
        public Conversation? Conversation { get; set; }

        public Guid SenderUserId { get; set; }
        public User? SenderUser { get; set; }

        public string Body { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false;
    }

}