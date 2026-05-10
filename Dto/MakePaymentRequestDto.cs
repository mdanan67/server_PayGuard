using System.ComponentModel.DataAnnotations;

namespace server.Dto
{
    public class MakePaymentRequestDto
    {
        [Required]
        public string Category { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
        public decimal Amount { get; set; }
    }
}
