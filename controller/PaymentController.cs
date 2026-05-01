using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Stripe;
using Stripe.Checkout;

namespace server.controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public PaymentController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("create-checkout-session")]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] AddMoneyRequest request)
        {
            try
            {
                if (request == null || request.Amount <= 0)
                {
                    return BadRequest(new { error = "Invalid amount" });
                }

                var userIdClaim =
                    User.FindFirst("userId")?.Value ??
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                    User.FindFirst("nameid")?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new { error = "User ID not found in token" });
                }

                if (!Guid.TryParse(userIdClaim, out Guid userId))
                {
                    return BadRequest(new
                    {
                        error = "User ID in token is not a valid GUID",
                        claimValue = userIdClaim
                    });
                }

                StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];

                if (string.IsNullOrEmpty(StripeConfiguration.ApiKey))
                {
                    return BadRequest(new { error = "Stripe secret key is missing" });
                }

                var options = new SessionCreateOptions
                {
                    Mode = "payment",
                    PaymentMethodTypes = new List<string> { "card" },

                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            Quantity = 1,
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                Currency = "usd",
                                UnitAmount = (long)(request.Amount * 100),
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = "Wallet Top-up"
                                }
                            }
                        }
                    },

                    SuccessUrl = "http://localhost:3000/payment-success?session_id={CHECKOUT_SESSION_ID}",
                    CancelUrl = "http://localhost:3000/dashboard",

                    Metadata = new Dictionary<string, string>
                    {
                        { "userId", userId.ToString() },
                        { "amount", request.Amount.ToString() }
                    }
                };

                var service = new SessionService();
                var session = await service.CreateAsync(options);

                return Ok(new { url = session.Url });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error = ex.Message,
                    detail = ex.InnerException?.Message
                });
            }
        }

        [HttpGet("verify-session")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifySession([FromQuery] string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                return BadRequest(new { error = "Session ID is required" });
            }

            try
            {
                StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];

                if (string.IsNullOrEmpty(StripeConfiguration.ApiKey))
                {
                    return BadRequest(new { error = "Stripe secret key is missing" });
                }

                var service = new SessionService();
                var session = await service.GetAsync(sessionId);

                if (session.PaymentStatus == "paid")
                {
                    Guid userId = Guid.Parse(session.Metadata["userId"]);
                    decimal amount = decimal.Parse(session.Metadata["amount"]);

                    // TODO: Add money to your database here
                    // Example:
                    // var user = await _context.Users.FindAsync(userId);
                    // user.Balance += amount;
                    // await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        success = true,
                        message = "Payment successful",
                        userId,
                        amount
                    });
                }

                return BadRequest(new
                {
                    success = false,
                    message = "Payment not completed"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error = ex.Message,
                    detail = ex.InnerException?.Message
                });
            }
        }
    }

    public class AddMoneyRequest
    {
        public decimal Amount { get; set; }
    }
}