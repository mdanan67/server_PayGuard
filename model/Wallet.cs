using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace server.model
{
    public class Wallet
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }

        public decimal Balance { get; set; }

        public decimal TotallSpend { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdaedAt { get; set; } = DateTime.UtcNow;


        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;

        public virtual ICollection<Transaction> SentTransactions { get; set; } = new List<Transaction>();
        public virtual ICollection<Transaction> ReceivedTransactions { get; set; } = new List<Transaction>();


    }
}