namespace server.model
{
    public class MonthlyExpense
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid UserId { get; set; }
        public User? User { get; set; }

        public Guid WalletId { get; set; }
        public Wallet? Wallet { get; set; }

        public Guid? TransactionId { get; set; }
        public Transaction? Transaction { get; set; }

        public string Category { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public int Month { get; set; }

        public int Year { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
