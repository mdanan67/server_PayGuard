namespace server.Dto
{
    public class CreateFundRequestDto
    {
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
