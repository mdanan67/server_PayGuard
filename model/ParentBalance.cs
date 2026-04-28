using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using server.Data;  // ✅ add this
namespace server.model
{
    public class ParentBalance
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public decimal TotalDeposited { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal TotalSentToChildren { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

    }
}