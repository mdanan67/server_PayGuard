using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Data;
using server.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using server.Dto;
using server.model;

namespace server.controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class ParentController : ControllerBase
    {
        private readonly AppDBContext _context;
        private readonly IMapper _mapper;
        private readonly IJwtTokenService _jwtTokenService;

        public ParentController(
            AppDBContext context,
            IMapper mapper,
            IJwtTokenService jwtTokenService)
        {
            _context = context;
            _mapper = mapper;
            _jwtTokenService = jwtTokenService;
        }


        [Authorize]
        [HttpGet("GetAllChil")]
        public async Task<IActionResult> GetAllChil()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            try
            {
                if (!Guid.TryParse(userId, out Guid userGuid))
                    return Unauthorized("invalid token ");

                var allchildinquary = await _context.FamilyMembers
                    .Where(e => e.ParentId == userGuid)
                    .Include(e => e.Child)
                    .ThenInclude(c => c.Wallet)
                    .Select(e => new
                    {
                        Id = e.Child.Id,
                        Name = e.Child.FirstName + " " + e.Child.LastName,
                        Email = e.Child.Email,
                        BirthDate = e.Child.BirthDate,
                        Gender = e.Child.Gender,

                        Wallet = e.Child.Wallet == null ? null : new
                        {
                            Id = e.Child.Wallet.Id,
                            UserId = e.Child.Wallet.UserId,
                            Balance = e.Child.Wallet.Balance,
                            TotalSpend = e.Child.Wallet.TotalSpend,
                            CreatedAt = e.Child.Wallet.CreatedAt
                        }
                    })
                    .ToListAsync();

                return Ok(new { allchildinquary });
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        [Authorize]
        [HttpGet("transaction")]
        public async Task<ActionResult> Transactions()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userId, out Guid parentGuid))
                return Unauthorized("invalid token");

            var parentWallet = await _context.Wallets
                .FirstOrDefaultAsync(wallet => wallet.UserId == parentGuid);

            if (parentWallet == null)
                return BadRequest(new { message = "Parent wallet not found" });

            var transactions = await _context.Transactions
                .Where(transaction =>
                    transaction.SenderWalletId == parentWallet.Id ||
                    transaction.ReceiverWalletId == parentWallet.Id)
                .OrderByDescending(transaction => transaction.CreatedAt)
                .Select(transaction => new
                {
                    transaction.Id,
                    transaction.SenderWalletId,
                    transaction.ReceiverWalletId,
                    transaction.Amount,
                    Type = transaction.Type.ToString(),
                    Status = transaction.Status.ToString(),
                    transaction.StripePaymentIntentId,
                    transaction.StripeChargeId,
                    transaction.FailureReason,
                    transaction.CreatedAt,
                    transaction.UpdatedAt
                })
                .ToListAsync();

            return Ok(new { transactions });
        }


        [Authorize]
        [HttpGet("GetWalletStaus")]
        public async Task<ActionResult> GetWalletStaus()
        {
            var UserID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            try
            {
                if (!Guid.TryParse(UserID, out Guid userGuid))
                    return Unauthorized("invalid token ");

                var wallet = await _context.Wallets.FirstOrDefaultAsync(id => id.UserId == userGuid);
                return Ok(wallet);
            }
            catch (System.Exception)
            {

                throw;
            }

        }

        [Authorize]
        [HttpPost("sendmoney")]
        public async Task<ActionResult> SendMoney([FromBody] SendMoneyDto request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userId, out Guid parentGuid))
                return Unauthorized("invalid token");

            if (request.ChildId == Guid.Empty)
                return BadRequest(new { message = "Please select a child" });

            if (request.Amount <= 0)
                return BadRequest(new { message = "Amount must be greater than zero" });

            var isParentOfChild = await _context.FamilyMembers.AnyAsync(familyMember =>
                familyMember.ParentId == parentGuid &&
                familyMember.ChildId == request.ChildId);

            if (!isParentOfChild)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "This child does not belong to this parent" });

            var parentWallet = await _context.Wallets
                .FirstOrDefaultAsync(wallet => wallet.UserId == parentGuid);

            if (parentWallet == null)
                return BadRequest(new { message = "Parent wallet not found" });

            var childWallet = await _context.Wallets
                .FirstOrDefaultAsync(wallet => wallet.UserId == request.ChildId);

            if (childWallet == null)
                return BadRequest(new { message = "Child wallet not found" });

            var parentBalance = parentWallet.Balance ?? 0m;

            if (parentBalance < request.Amount)
                return BadRequest(new { message = "Insufficient parent wallet balance" });

            await using var dbTransaction = await _context.Database.BeginTransactionAsync();

            parentWallet.Balance = parentBalance - request.Amount;
            parentWallet.TotalSpend = (parentWallet.TotalSpend ?? 0m) + request.Amount;
            childWallet.Balance = (childWallet.Balance ?? 0m) + request.Amount;

            var transaction = new server.model.Transaction
            {
                SenderWalletId = parentWallet.Id,
                ReceiverWalletId = childWallet.Id,
                Amount = request.Amount,
                Type = TransactionType.Transfer,
                Status = TransactionStatus.Success,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Transactions.AddAsync(transaction);
            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return Ok(new
            {
                message = "Money sent successfully",
                transactionId = transaction.Id,
                parentWallet = new
                {
                    parentWallet.Id,
                    parentWallet.UserId,
                    parentWallet.Balance,
                    parentWallet.TotalSpend
                },
                childWallet = new
                {
                    childWallet.Id,
                    childWallet.UserId,
                    childWallet.Balance,
                    childWallet.TotalSpend
                }
            });
        }


        [Authorize]
        [HttpGet("FundRequests")]
        public async Task<ActionResult> GetFundRequests()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userId, out Guid parentGuid))
                return Unauthorized("invalid token");

            var requests = await _context.FundRequests
                .Where(request => request.ParentId == parentGuid)
                .Include(request => request.Child)
                .OrderBy(request => request.Status == FundRequestStatus.Pending ? 0 : 1)
                .ThenByDescending(request => request.CreatedAt)
                .Select(request => new
                {
                    request.Id,
                    request.ChildId,
                    ChildName = request.Child.FirstName + " " + request.Child.LastName,
                    ChildEmail = request.Child.Email,
                    request.Amount,
                    request.Reason,
                    Status = request.Status.ToString(),
                    request.CreatedAt,
                    request.UpdatedAt
                })
                .ToListAsync();

            return Ok(new { requests });
        }

        [Authorize]
        [HttpPost("FundRequests/{requestId}/cancel")]
        public async Task<ActionResult> CancelFundRequest(Guid requestId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userId, out Guid parentGuid))
                return Unauthorized("invalid token");

            var fundRequest = await _context.FundRequests
                .FirstOrDefaultAsync(request => request.Id == requestId && request.ParentId == parentGuid);

            if (fundRequest == null)
                return NotFound(new { message = "Fund request not found" });

            if (fundRequest.Status != FundRequestStatus.Pending)
                return BadRequest(new { message = "Only pending requests can be canceled" });

            fundRequest.Status = FundRequestStatus.Canceled;
            fundRequest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Fund request canceled",
                requestId = fundRequest.Id,
                Status = fundRequest.Status.ToString()
            });
        }

        [Authorize]
        [HttpPost("FundRequests/{requestId}/approve")]
        public async Task<ActionResult> ApproveFundRequest(Guid requestId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userId, out Guid parentGuid))
                return Unauthorized("invalid token");

            var fundRequest = await _context.FundRequests
                .FirstOrDefaultAsync(request => request.Id == requestId && request.ParentId == parentGuid);

            if (fundRequest == null)
                return NotFound(new { message = "Fund request not found" });

            if (fundRequest.Status != FundRequestStatus.Pending)
                return BadRequest(new { message = "Only pending requests can be approved" });

            var isParentOfChild = await _context.FamilyMembers.AnyAsync(familyMember =>
                familyMember.ParentId == parentGuid &&
                familyMember.ChildId == fundRequest.ChildId);

            if (!isParentOfChild)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "This child does not belong to this parent" });

            var parentWallet = await _context.Wallets
                .FirstOrDefaultAsync(wallet => wallet.UserId == parentGuid);

            if (parentWallet == null)
                return BadRequest(new { message = "Parent wallet not found" });

            var childWallet = await _context.Wallets
                .FirstOrDefaultAsync(wallet => wallet.UserId == fundRequest.ChildId);

            if (childWallet == null)
                return BadRequest(new { message = "Child wallet not found" });

            var parentBalance = parentWallet.Balance ?? 0m;

            if (parentBalance < fundRequest.Amount)
                return BadRequest(new { message = "Insufficient parent wallet balance" });

            await using var dbTransaction = await _context.Database.BeginTransactionAsync();

            var now = DateTime.UtcNow;

            parentWallet.Balance = parentBalance - fundRequest.Amount;
            parentWallet.TotalSpend = (parentWallet.TotalSpend ?? 0m) + fundRequest.Amount;
            childWallet.Balance = (childWallet.Balance ?? 0m) + fundRequest.Amount;

            fundRequest.Status = FundRequestStatus.Approved;
            fundRequest.UpdatedAt = now;

            var transaction = new server.model.Transaction
            {
                SenderWalletId = parentWallet.Id,
                ReceiverWalletId = childWallet.Id,
                Amount = fundRequest.Amount,
                Type = TransactionType.Transfer,
                Status = TransactionStatus.Success,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _context.Transactions.AddAsync(transaction);
            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return Ok(new
            {
                message = "Fund request approved",
                requestId = fundRequest.Id,
                transactionId = transaction.Id,
                parentBalance = parentWallet.Balance,
                childBalance = childWallet.Balance
            });
        }

        [Authorize]
        [HttpGet("GetSpendingLimit/{childId}")]
        public async Task<ActionResult> GetSpendingLimit(Guid childId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userId, out Guid parentGuid))
                return Unauthorized("invalid token");

            if (childId == Guid.Empty)
                return BadRequest(new { message = "Please select a child" });

            var isParentOfChild = await _context.FamilyMembers.AnyAsync(familyMember =>
                familyMember.ParentId == parentGuid &&
                familyMember.ChildId == childId);

            if (!isParentOfChild)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "This child does not belong to this parent" });

            var spendingLimit = await _context.SpendingLimits
                .Where(limit => limit.UserId == childId)
                .Select(limit => new
                {
                    limit.Id,
                    limit.UserId,
                    limit.Food,
                    limit.Education,
                    limit.Transport,
                    limit.Entertainment,
                    limit.Shopping,
                    limit.Subscriptions,
                    limit.Mobile,
                    limit.Others
                })
                .FirstOrDefaultAsync();

            if (spendingLimit == null)
                return Ok(new { spendingLimit = (object?)null });

            return Ok(new { spendingLimit });
        }

        [Authorize]
        [HttpPost("SetSpendingLimit")]
        public async Task<ActionResult> SetSpendingLimit([FromBody] SpendingLimitDto request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userId, out Guid parentGuid))
                return Unauthorized("invalid token");

            if (request.ChildId == Guid.Empty)
                return BadRequest(new { message = "Please select a child" });

            var hasLimitValue =
                request.Food.HasValue ||
                request.Education.HasValue ||
                request.Transport.HasValue ||
                request.Entertainment.HasValue ||
                request.Shopping.HasValue ||
                request.Subscriptions.HasValue ||
                request.Mobile.HasValue ||
                request.Others.HasValue;

            if (!hasLimitValue)
                return BadRequest(new { message = "Please provide at least one spending limit" });

            if (
                (request.Food.HasValue && request.Food.Value < 0) ||
                (request.Education.HasValue && request.Education.Value < 0) ||
                (request.Transport.HasValue && request.Transport.Value < 0) ||
                (request.Entertainment.HasValue && request.Entertainment.Value < 0) ||
                (request.Shopping.HasValue && request.Shopping.Value < 0) ||
                (request.Subscriptions.HasValue && request.Subscriptions.Value < 0) ||
                (request.Mobile.HasValue && request.Mobile.Value < 0) ||
                (request.Others.HasValue && request.Others.Value < 0))
            {
                return BadRequest(new { message = "Spending limits cannot be negative" });
            }

            var isParentOfChild = await _context.FamilyMembers.AnyAsync(familyMember =>
                familyMember.ParentId == parentGuid &&
                familyMember.ChildId == request.ChildId);

            if (!isParentOfChild)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "This child does not belong to this parent" });

            var spendingLimit = await _context.SpendingLimits
                .FirstOrDefaultAsync(limit => limit.UserId == request.ChildId);

            if (spendingLimit == null)
            {
                spendingLimit = new SpendingLimit
                {
                    UserId = request.ChildId
                };

                await _context.SpendingLimits.AddAsync(spendingLimit);
            }

            if (request.Food.HasValue)
                spendingLimit.Food = request.Food.Value;

            if (request.Education.HasValue)
                spendingLimit.Education = request.Education.Value;

            if (request.Transport.HasValue)
                spendingLimit.Transport = request.Transport.Value;

            if (request.Entertainment.HasValue)
                spendingLimit.Entertainment = request.Entertainment.Value;

            if (request.Shopping.HasValue)
                spendingLimit.Shopping = request.Shopping.Value;

            if (request.Subscriptions.HasValue)
                spendingLimit.Subscriptions = request.Subscriptions.Value;

            if (request.Mobile.HasValue)
                spendingLimit.Mobile = request.Mobile.Value;

            if (request.Others.HasValue)
                spendingLimit.Others = request.Others.Value;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Spending limit saved successfully",
                spendingLimit = new
                {
                    spendingLimit.Id,
                    spendingLimit.UserId,
                    spendingLimit.Food,
                    spendingLimit.Education,
                    spendingLimit.Transport,
                    spendingLimit.Entertainment,
                    spendingLimit.Shopping,
                    spendingLimit.Subscriptions,
                    spendingLimit.Mobile,
                    spendingLimit.Others
                }
            });
        }


    }
}
