using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Data;
using server.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
        public async Task<IActionResult> GetAllClaimsAndCookies()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            try
            {
                if (!Guid.TryParse(userId, out Guid userGuid))
                    return Unauthorized("invalid token ");

                var allchildinquary = await _context.FamilyMembers
                                    .Where(e => e.ParentId == userGuid)
                                    .Include(e => e.Child)
                                    .Select(e => new
                                    {
                                        Id = e.Child.Id,
                                        Name = e.Child.FirstName + " " + e.Child.LastName,
                                        Email = e.Child.Email,
                                        BirthDate = e.Child.BirthDate,
                                        Gender = e.Child.Gender,
                                    })
                                    .ToListAsync();

                return Ok(new { allchildinquary });
            }
            catch (System.Exception)
            {

                throw;
            }

        }






    }
}