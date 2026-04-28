using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace server.model
{
    public class Transaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid? SenderWalletId { get; set; }
        public Wallet? SenderWallet { get; set; }

        public Guid? ReceiverWalletId { get; set; }
        public Wallet? ReceiverWallet { get; set; }

        public decimal Amount { get; set; }

        public TransactionType Type { get; set; }

        public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

        public string? StripePaymentIntentId { get; set; }

        public string? StripeChargeId { get; set; }

        public string? FailureReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
    public enum TransactionType
    {
        Topup,
        Transfer,
        Payment
    }

    public enum TransactionStatus
    {
        Pending,
        Success,
        Failed
    }

}

