using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Dto;
using server.model;
using server.Services;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace server.controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChildController : ControllerBase
    {
        private AppDBContext _context;
        private IMapper _mapper;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IConfiguration _configuration;

        public ChildController(AppDBContext context, IMapper mapper, IJwtTokenService jwtTokenService, IConfiguration configuration)
        {
            _context = context;
            _mapper = mapper;
            _jwtTokenService = jwtTokenService;
            _configuration = configuration;
        }

        [Authorize]
        [HttpGet("GetWalletStatus")]
        public async Task<ActionResult> GetWalletStatus()
        {
            // if (!TryGetCurrentUserId(out var userId, out var authError))
            //     return authError!;



            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid.TryParse(claim, out var userId);

            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
            {
                return Ok(new
                {
                    balance = 0m,
                    totalSpend = 0m,
                    currentMonthExpense = 0m,
                    monthlyExpenseByCategory = Array.Empty<object>()
                });
            }

            var now = DateTime.UtcNow;

            var monthlyExpenseByCategory = await _context.MonthlyExpenses
                .Where(expense =>
                    expense.UserId == userId &&
                    expense.Month == now.Month &&
                    expense.Year == now.Year)
                .GroupBy(expense => expense.Category)
                .Select(group => new
                {
                    category = group.Key,
                    amount = group.Sum(expense => expense.Amount)
                })
                .OrderByDescending(expense => expense.amount)
                .ToListAsync();

            var currentMonthExpense = monthlyExpenseByCategory.Sum(expense => expense.amount);

            return Ok(new
            {
                walletId = wallet.Id,
                balance = wallet.Balance ?? 0m,
                totalSpend = wallet.TotalSpend ?? 0m,
                currentMonthExpense,
                month = now.Month,
                year = now.Year,
                monthlyExpenseByCategory
            });
        }

        [Authorize]
        [HttpGet("GetCategoryBudget")]
        public async Task<ActionResult> GetCategoryBudget()
        {
            if (!TryGetCurrentUserId(out var userId, out var authError))
                return authError!;

            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == userId);

            var walletBalance = wallet?.Balance ?? 0m;
            var now = DateTime.UtcNow;

            var monthlyExpenseByCategory = await _context.MonthlyExpenses
                .Where(expense =>
                    expense.UserId == userId &&
                    expense.Month == now.Month &&
                    expense.Year == now.Year)
                .GroupBy(expense => expense.Category)
                .Select(group => new
                {
                    Category = group.Key,
                    Amount = group.Sum(expense => expense.Amount)
                })
                .ToListAsync();

            var spentByCategory = monthlyExpenseByCategory
                .ToDictionary(expense => expense.Category, expense => expense.Amount);

            var spendingLimit = await _context.SpendingLimits
                .FirstOrDefaultAsync(limit => limit.UserId == userId);

            if (spendingLimit == null)
            {
                return Ok(new
                {
                    walletBalance,
                    month = now.Month,
                    year = now.Year,
                    categories = Array.Empty<object>()
                });
            }

            var categories = new[]
            {
                "Food",
                "Education",
                "Transport",
                "Entertainment",
                "Shopping",
                "Subscriptions",
                "Mobile",
                "Others"
            };

            var categoryBudgets = categories.Select(category =>
            {
                var limit = GetCategoryLimit(spendingLimit, category) ?? 0m;
                var spent = spentByCategory.GetValueOrDefault(category, 0m);
                var remainingLimit = Math.Max(0m, limit - spent);
                var available = Math.Min(remainingLimit, walletBalance);
                var percentUsed = limit > 0m ? Math.Min(100m, decimal.Round(spent / limit * 100m, 2)) : 0m;

                return new
                {
                    category,
                    limit,
                    spent,
                    remainingLimit,
                    available,
                    percentUsed
                };
            });

            return Ok(new
            {
                walletBalance,
                month = now.Month,
                year = now.Year,
                categories = categoryBudgets
            });
        }

        [Authorize]
        [HttpGet("transactions")]
        public async Task<ActionResult> GetTransactions([FromQuery] string? from, [FromQuery] string? to)
        {
            if (!TryGetCurrentUserId(out var userId, out var authError))
                return authError!;

            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
            {
                return Ok(new
                {
                    transactions = Array.Empty<object>()
                });
            }

            if (!TryParseDateFilter(from, out var fromDate))
                return BadRequest(new { message = "Invalid from date" });

            if (!TryParseDateFilter(to, out var toDate))
                return BadRequest(new { message = "Invalid to date" });

            if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
                return BadRequest(new { message = "From date cannot be after To date" });

            var query = _context.Transactions
                .Where(transaction =>
                    transaction.SenderWalletId == wallet.Id ||
                    transaction.ReceiverWalletId == wallet.Id);

            if (fromDate.HasValue)
            {
                query = query.Where(transaction => transaction.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                var endDate = toDate.Value.AddDays(1).AddTicks(-1);
                query = query.Where(transaction => transaction.CreatedAt <= endDate);
            }

            var transactions = await query
                .OrderByDescending(transaction => transaction.CreatedAt)
                .Select(transaction => new
                {
                    transaction.Id,
                    transaction.SenderWalletId,
                    SenderName = transaction.SenderWallet == null || transaction.SenderWallet.User == null
                        ? null
                        : transaction.SenderWallet.User.FirstName + " " + transaction.SenderWallet.User.LastName,
                    transaction.ReceiverWalletId,
                    ReceiverName = transaction.ReceiverWallet == null || transaction.ReceiverWallet.User == null
                        ? null
                        : transaction.ReceiverWallet.User.FirstName + " " + transaction.ReceiverWallet.User.LastName,
                    transaction.Amount,
                    transaction.Category,
                    Type = transaction.Type.ToString(),
                    Status = transaction.Status.ToString(),
                    Direction = transaction.ReceiverWalletId == wallet.Id ? "Received" : "Spent",
                    transaction.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                transactions
            });
        }

        private static bool TryParseDateFilter(string? value, out DateTime? date)
        {
            date = null;

            if (string.IsNullOrWhiteSpace(value))
                return true;

            if (!DateTime.TryParseExact(
                    value,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedDate))
            {
                return false;
            }

            date = DateTime.SpecifyKind(parsedDate.Date, DateTimeKind.Utc);
            return true;
        }

        [Authorize]
        [HttpPost("FundRequests")]
        public async Task<ActionResult> CreateFundRequest([FromBody] CreateFundRequestDto request)
        {
            if (request == null || request.Amount <= 0)
                return BadRequest(new { message = "Amount must be greater than zero" });

            if (string.IsNullOrWhiteSpace(request.Reason))
                return BadRequest(new { message = "Please write a reason for the request" });

            if (!TryGetCurrentUserId(out var childId, out var authError))
                return authError!;

            var familyMember = await _context.FamilyMembers
                .FirstOrDefaultAsync(member => member.ChildId == childId);

            if (familyMember == null)
                return BadRequest(new { message = "No parent account is linked to this child" });

            var fundRequest = new FundRequest
            {
                ChildId = childId,
                ParentId = familyMember.ParentId,
                Amount = request.Amount,
                Reason = request.Reason.Trim(),
                Status = FundRequestStatus.Pending
            };

            await _context.FundRequests.AddAsync(fundRequest);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Money request sent to parent",
                request = new
                {
                    fundRequest.Id,
                    fundRequest.ChildId,
                    fundRequest.ParentId,
                    fundRequest.Amount,
                    fundRequest.Reason,
                    Status = fundRequest.Status.ToString(),
                    fundRequest.CreatedAt
                }
            });
        }

        [Authorize]
        [HttpPost("MakePayment")]
        public async Task<ActionResult> MakePayment([FromBody] MakePaymentRequestDto request)
        {
            try
            {
                if (request == null || request.Amount <= 0)
                    return BadRequest(new { error = "Invalid amount" });

                if (!TryGetCurrentUserId(out var userId, out var authError))
                    return authError!;

                var category = NormalizeCategory(request.Category);
                if (string.IsNullOrEmpty(category))
                    return BadRequest(new { error = "Invalid payment category" });

                var spendingLimit = await _context.SpendingLimits
                    .FirstOrDefaultAsync(limit => limit.UserId == userId);

                if (spendingLimit == null)
                    return BadRequest(new { error = "No monthly spending limit was found for this child" });

                var categoryLimit = GetCategoryLimit(spendingLimit, category);
                if (categoryLimit == null)
                    return BadRequest(new { error = "Monthly spending limit was not found for this category" });


                var wallet = await _context.Wallets
                    .FirstOrDefaultAsync(w => w.UserId == userId);

                if (wallet == null)
                    return BadRequest(new { error = "Child wallet was not found" });

                var walletBalance = wallet.Balance ?? 0m;
                if (walletBalance < request.Amount)
                    return BadRequest(new { error = "Insufficient wallet balance", balance = walletBalance });

                var now = DateTime.UtcNow;
                var currentMonthExpense = await _context.MonthlyExpenses
                    .Where(expense =>
                        expense.UserId == userId &&
                        expense.Category == category &&
                        expense.Month == now.Month &&
                        expense.Year == now.Year)
                    .SumAsync(expense => expense.Amount);

                if (currentMonthExpense + request.Amount > categoryLimit.Value)
                {
                    return BadRequest(new
                    {
                        error = "Monthly category spending limit exceeded",
                        category,
                        monthlyLimit = categoryLimit.Value,
                        currentMonthExpense,
                        requestedAmount = request.Amount,
                        remainingLimit = Math.Max(0m, categoryLimit.Value - currentMonthExpense)
                    });
                }

                StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
                if (string.IsNullOrEmpty(StripeConfiguration.ApiKey))
                    return BadRequest(new { error = "Stripe secret key is missing" });

                var sessionOptions = new SessionCreateOptions
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
                                    Name = $"{category} payment"
                                }
                            }
                        }
                    },
                    SuccessUrl = "http://localhost:3000/payment-success?type=child-payment&session_id={CHECKOUT_SESSION_ID}",
                    CancelUrl = "http://localhost:3000/dashboard"
                };

                var sessionService = new SessionService();
                var session = await sessionService.CreateAsync(sessionOptions);

                var pendingTransaction = new Transaction
                {
                    SenderWalletId = wallet.Id,
                    Amount = request.Amount,
                    Category = category,
                    Type = TransactionType.Payment,
                    Status = TransactionStatus.Pending,
                    StripeCheckoutSessionId = session.Id,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await _context.Transactions.AddAsync(pendingTransaction);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    url = session.Url
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error = "Failed to start payment",
                    detail = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        [AllowAnonymous]
        [HttpGet("VerifyPaymentSession")]
        public async Task<ActionResult> VerifyPaymentSession([FromQuery] string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return BadRequest(new { error = "Session ID is required" });

            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
            if (string.IsNullOrEmpty(StripeConfiguration.ApiKey))
                return BadRequest(new { error = "Stripe secret key is missing" });

            var sessionService = new SessionService();
            var session = await sessionService.GetAsync(sessionId);

            if (session.PaymentStatus != "paid")
                return BadRequest(new { message = "Payment not completed" });

            var transaction = await _context.Transactions
                .Include(t => t.SenderWallet)
                .FirstOrDefaultAsync(t => t.StripeCheckoutSessionId == session.Id);

            if (transaction == null)
                return NotFound(new { error = "Payment transaction was not found" });

            if (transaction.Status == TransactionStatus.Success)
            {
                return Ok(new
                {
                    message = "Payment already verified"
                });
            }

            await using var dbTransaction = await _context.Database.BeginTransactionAsync();

            var wallet = transaction.SenderWallet;
            if (wallet == null && transaction.SenderWalletId.HasValue)
            {
                wallet = await _context.Wallets
                    .FirstOrDefaultAsync(w => w.Id == transaction.SenderWalletId.Value);
            }

            if (wallet == null)
                return BadRequest(new { error = "Child wallet was not found" });

            var category = NormalizeCategory(transaction.Category);
            if (string.IsNullOrEmpty(category))
                return BadRequest(new { error = "Payment category was not found" });

            var spendingLimit = await _context.SpendingLimits
                .FirstOrDefaultAsync(limit => limit.UserId == wallet.UserId);

            var categoryLimit = spendingLimit == null ? null : GetCategoryLimit(spendingLimit, category);
            if (categoryLimit == null)
                return BadRequest(new { error = "Monthly spending limit was not found for this category" });

            var now = DateTime.UtcNow;
            var currentMonthExpense = await _context.MonthlyExpenses
                .Where(expense =>
                    expense.UserId == wallet.UserId &&
                    expense.Category == category &&
                    expense.Month == now.Month &&
                    expense.Year == now.Year)
                .SumAsync(expense => expense.Amount);

            if (currentMonthExpense + transaction.Amount > categoryLimit.Value)
            {
                transaction.Status = TransactionStatus.Failed;
                transaction.FailureReason = "Monthly category spending limit exceeded before payment verification";
                transaction.UpdatedAt = now;
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return BadRequest(new { error = transaction.FailureReason });
            }

            var walletBalance = wallet.Balance ?? 0m;
            if (walletBalance < transaction.Amount)
            {
                transaction.Status = TransactionStatus.Failed;
                transaction.FailureReason = "Insufficient wallet balance before payment verification";
                transaction.UpdatedAt = now;
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return BadRequest(new { error = transaction.FailureReason });
            }

            wallet.Balance = walletBalance - transaction.Amount;
            wallet.TotalSpend = (wallet.TotalSpend ?? 0m) + transaction.Amount;

            transaction.Status = TransactionStatus.Success;
            transaction.Category = category;
            transaction.StripePaymentIntentId = session.PaymentIntentId;
            transaction.UpdatedAt = now;

            await _context.MonthlyExpenses.AddAsync(new MonthlyExpense
            {
                UserId = wallet.UserId,
                WalletId = wallet.Id,
                TransactionId = transaction.Id,
                Category = category,
                Amount = transaction.Amount,
                Month = now.Month,
                Year = now.Year,
                CreatedAt = now
            });

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return Ok(new
            {
                message = "Payment successful"
            });
        }

        private bool TryGetCurrentUserId(out Guid userId, out ActionResult? error)
        {
            userId = Guid.Empty;
            error = null;

            var userIdClaim =
                User.FindFirst("userId")?.Value ??
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                User.FindFirst("nameid")?.Value;

            if (string.IsNullOrWhiteSpace(userIdClaim))
            {
                error = Unauthorized(new { error = "User ID not found in token" });
                return false;
            }

            if (!Guid.TryParse(userIdClaim, out userId))
            {
                error = BadRequest(new { error = "User ID in token is not a valid GUID", claimValue = userIdClaim });
                return false;
            }

            return true;
        }

        private static string NormalizeCategory(string? category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return string.Empty;

            return category.Trim().ToLowerInvariant() switch
            {
                "food" => "Food",
                "education" => "Education",
                "transport" => "Transport",
                "entertainment" => "Entertainment",
                "shopping" => "Shopping",
                "subscriptions" => "Subscriptions",
                "mobile" => "Mobile",
                "others" or "other" => "Others",
                _ => string.Empty
            };
        }

        private static decimal? GetCategoryLimit(SpendingLimit spendingLimit, string category)
        {
            return category switch
            {
                "Food" => (decimal)spendingLimit.Food,
                "Education" => (decimal)spendingLimit.Education,
                "Transport" => (decimal)spendingLimit.Transport,
                "Entertainment" => (decimal)spendingLimit.Entertainment,
                "Shopping" => (decimal)spendingLimit.Shopping,
                "Subscriptions" => (decimal)spendingLimit.Subscriptions,
                "Mobile" => (decimal)spendingLimit.Mobile,
                "Others" => (decimal)spendingLimit.Others,
                _ => null
            };
        }


    }
}
