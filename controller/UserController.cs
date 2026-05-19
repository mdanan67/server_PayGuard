
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using server.Dto;
using server.model;
using server.Dto.LoginDto;
using server.Dto.LoginResponseDto;
using server.Data;
using server.Services;
using server.Dto.child;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace server.controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private AppDBContext _context;
        private IMapper _mapper;
        private IJwtTokenService _jwtTokenService;


        public UserController(AppDBContext context, IMapper mapper, IJwtTokenService jwtTokenService)
        {
            _context = context;
            _mapper = mapper;
            _jwtTokenService = jwtTokenService;
        }
        [HttpPost]
        [Route("registation")]
        public async Task<ActionResult> Registratio([FromBody] ParentRegistrationDto user)
        {

            var exists = await _context.Users.AnyAsync(u => u.Email == user.Email);
            if (exists)
                return BadRequest(new { message = "Email already registered" });

            var newuser = _mapper.Map<User>(user);
            newuser.Role = "parent";
            newuser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.Password);
            newuser.CreatedAt = DateTime.UtcNow;
            newuser.UpdatedAt = DateTime.UtcNow;

            await _context.Users.AddAsync(newuser);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Account created successfully" });
        }

        [HttpPost]
        [Route("login")]
        public async Task<ActionResult> Login([FromBody] LoginDto request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            var isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

            if (!isPasswordValid)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            var token = _jwtTokenService.GenerateToken(user.Id, user.Email, user.Role);

            Response.Cookies.Append("jwt", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddDays(7),
                Domain = "localhost"
            });

            var response = new LoginResponseDto
            {
                Id = user.Id,
                Role = user.Role,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                ProfileImage = user.Profile_image
            };

            return Ok(response);
        }

        [HttpPost]
        [Route("logout")]
        public ActionResult Logout()
        {
            Response.Cookies.Delete("jwt", new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Domain = "localhost"
            });

            return Ok(new { message = "Logged out successfully" });
        }

        [Authorize]
        [HttpGet]
        [Route("profile")]
        public async Task<ActionResult> GetProfile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.Phone,
                ProfileImage = user.Profile_image,
                user.Role,
                user.Gender,
                user.BirthDate
            });
        }

        [Authorize]
        [HttpPost]
        [Route("EditProfile")]
        public async Task<ActionResult> EditProfile([FromBody] EditProfileDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var emailAlreadyUsed = await _context.Users.AnyAsync(u =>
                u.Email == request.Email && u.Id != userId);

            if (emailAlreadyUsed)
            {
                return BadRequest(new { message = "Email already registered" });
            }

            user.FirstName = request.FirstName.Trim();
            user.LastName = request.LastName.Trim();
            user.Email = request.Email.Trim();
            user.Phone = request.Phone?.Trim() ?? string.Empty;
            user.Profile_image = request.ProfileImage ?? string.Empty;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Profile updated successfully",
                user = new
                {
                    user.Id,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.Phone,
                    ProfileImage = user.Profile_image,
                    user.Role
                }
            });
        }


        [Authorize]
        [HttpPost]
        [Route("childRegistration")]
        public async Task<ActionResult> ChildRegistration([FromBody] RegistrationChildDto request)
        {
            var parentIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(parentIdClaim))
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            var parentId = Guid.Parse(parentIdClaim);

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (existingUser != null)
            {
                return BadRequest(new { message = "Email already registered" });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var newUser = _mapper.Map<User>(request);

                newUser.Role = "child";
                newUser.CreatedAt = DateTime.UtcNow;
                newUser.UpdatedAt = DateTime.UtcNow;
                newUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                await _context.Users.AddAsync(newUser);
                await _context.SaveChangesAsync();

                var childWallet = new Wallet
                {
                    UserId = newUser.Id,
                    Balance = 0m,
                    TotalSpend = 0m,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Wallets.AddAsync(childWallet);
                await _context.SaveChangesAsync();

                var familyMember = new FamilyMember
                {
                    ParentId = parentId,
                    ChildId = newUser.Id,
                };

                await _context.FamilyMembers.AddAsync(familyMember);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Child registered successfully",
                    childId = newUser.Id,
                    walletId = childWallet.Id,
                    LinkedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return StatusCode(500, new
                {
                    message = ex.Message,
                    details = ex.InnerException?.Message
                });
            }
        }

    }

}
