using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.model;
using System.Globalization;
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
        private readonly AppDBContext _context;

        public PaymentController(IConfiguration configuration, AppDBContext context)
        {
            _configuration = configuration;
            _context = context;
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
                                UnitAmount = (long)decimal.Round(request.Amount * 100, 0),
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
                        { "amount", request.Amount.ToString(CultureInfo.InvariantCulture) }
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
                    if (!session.Metadata.TryGetValue("userId", out var userIdValue) ||
                        !Guid.TryParse(userIdValue, out Guid userId))
                    {
                        return BadRequest(new { error = "Valid user ID was not found in the Stripe session" });
                    }

                    if (!session.Metadata.TryGetValue("amount", out var amountValue) ||
                        !decimal.TryParse(amountValue, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount) ||
                        amount <= 0)
                    {
                        return BadRequest(new { error = "Valid amount was not found in the Stripe session" });
                    }

                    var stripePaymentId = session.PaymentIntentId ?? session.Id;

                    using var dbTransaction = await _context.Database.BeginTransactionAsync();

                    var existingTransaction = await _context.Transactions
                        .Include(t => t.ReceiverWallet)
                        .FirstOrDefaultAsync(t => t.StripePaymentIntentId == stripePaymentId);

                    if (existingTransaction != null)
                    {
                        await dbTransaction.CommitAsync();

                        return Ok(new
                        {
                            success = true,
                            message = "Payment already verified",
                            userId,
                            amount = existingTransaction.Amount,
                            newBalance = existingTransaction.ReceiverWallet?.Balance ?? 0m
                        });
                    }

                    var wallet = await _context.Wallets
                        .FirstOrDefaultAsync(w => w.UserId == userId);

                    if (wallet == null)
                    {
                        wallet = new Wallet
                        {
                            UserId = userId,
                            Balance = 0m,
                            TotalSpend = 0m,
                            CreatedAt = DateTime.UtcNow
                        };

                        await _context.Wallets.AddAsync(wallet);
                    }

                    wallet.Balance = (wallet.Balance ?? 0m) + amount;

                    var topupTransaction = new Transaction
                    {
                        ReceiverWalletId = wallet.Id,
                        Amount = amount,
                        Type = TransactionType.Topup,
                        Status = TransactionStatus.Success,
                        StripePaymentIntentId = stripePaymentId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _context.Transactions.AddAsync(topupTransaction);
                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();

                    return Ok(new
                    {
                        success = true,
                        message = "Payment successful",
                        userId,
                        amount,
                        newBalance = wallet.Balance
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
