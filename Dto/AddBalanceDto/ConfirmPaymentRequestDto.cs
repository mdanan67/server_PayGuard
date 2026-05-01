using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace server.Dto.AddBalanceDto
{
    public class ConfirmPaymentRequestDto
    {
        [Required(ErrorMessage = "Payment Intent ID is required")]
        public string PaymentIntentId { get; set; } = string.Empty;
    }
}
