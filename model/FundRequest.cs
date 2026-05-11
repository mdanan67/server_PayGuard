namespace server.model
{
    public class FundRequest
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ChildId { get; set; }
        public User Child { get; set; } = null!;

        public Guid ParentId { get; set; }
        public User Parent { get; set; } = null!;

        public decimal Amount { get; set; }

        public string Reason { get; set; } = string.Empty;

        public FundRequestStatus Status { get; set; } = FundRequestStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }

    public enum FundRequestStatus
    {
        Pending,
        Approved,
        Canceled
    }
}
