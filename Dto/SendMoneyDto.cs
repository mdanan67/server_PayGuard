using System.ComponentModel.DataAnnotations;

namespace server.Dto
{
    public class SendMoneyDto
    {
        [Required(ErrorMessage = "ChildId is required")]
        public Guid ChildId { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
        public decimal Amount { get; set; }
    }
}
