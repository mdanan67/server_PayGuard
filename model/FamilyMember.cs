using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace server.model
{
    public class FamilyMember
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ParentId { get; set; }
        public User Parent { get; set; } = null!;

        public Guid ChildId { get; set; }
        public User Child { get; set; } = null!;

        public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
    }
}