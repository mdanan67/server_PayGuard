using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Data;
using server.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Transactions;

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
            var userID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userID, out Guid userGuid))
                return Unauthorized("invalid token ");

            var Transactions = await _context.Transactions.FirstOrDefaultAsync(parent => parent.SenderWalletId != userGuid);

            return Ok(Transactions);
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


    }
}