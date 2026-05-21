using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Dto;
using server.Services;
using server.model;

namespace server.controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDBContext _context;
        private readonly IEmailService _emailService;

        private static readonly ConcurrentDictionary<string, PasswordResetState> ResetStates =
            new ConcurrentDictionary<string, PasswordResetState>(StringComparer.OrdinalIgnoreCase);

        public AuthController(AppDBContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpPost]
        [Route("send-reset-otp")]
        public async Task<ActionResult> SendResetOtp([FromBody] SendResetOtpDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email.Trim());
            if (user == null)
            {
                return NotFound(new { message = "No account found with this email." });
            }

            var otp = new Random().Next(100000, 999999).ToString();
            var state = new PasswordResetState
            {
                Email = request.Email.Trim(),
                Otp = otp,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                Verified = false
            };

            ResetStates.AddOrUpdate(request.Email.Trim(), state, (_, __) => state);

            var subject = "Your PgGuard password reset code";
            var body = $"<p>Your PgGuard password reset code is:</p><h2>{otp}</h2><p>This code expires in 15 minutes.</p>";

            try
            {
                await _emailService.SendEmailAsync(request.Email.Trim(), subject, body);
            }
            catch (SmtpException ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = "Unable to send email. Check SMTP settings.", detail });
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = "Unable to send email. Check SMTP settings.", detail });
            }

            return Ok(new { message = "OTP sent to your email address." });
        }

        [HttpPost]
        [Route("verify-otp")]
        public ActionResult VerifyOtp([FromBody] VerifyOtpDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!ResetStates.TryGetValue(request.Email.Trim(), out var state))
            {
                return BadRequest(new { message = "Please request a password reset first." });
            }

            if (state.ExpiresAt < DateTime.UtcNow)
            {
                ResetStates.TryRemove(request.Email.Trim(), out _);
                return BadRequest(new { message = "OTP has expired. Please request a new code." });
            }

            if (state.Otp != request.Otp.Trim())
            {
                return BadRequest(new { message = "The OTP is invalid." });
            }

            state.Verified = true;
            ResetStates[request.Email.Trim()] = state;

            return Ok(new { message = "OTP verified successfully." });
        }

        [HttpPost]
        [Route("reset-password")]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!ResetStates.TryGetValue(request.Email.Trim(), out var state) || !state.Verified)
            {
                return BadRequest(new { message = "OTP verification is required before resetting your password." });
            }

            if (state.ExpiresAt < DateTime.UtcNow)
            {
                ResetStates.TryRemove(request.Email.Trim(), out _);
                return BadRequest(new { message = "OTP has expired. Please request a new code." });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email.Trim());
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            ResetStates.TryRemove(request.Email.Trim(), out _);

            return Ok(new { message = "Password has been reset successfully." });
        }

        private class PasswordResetState
        {
            public string Email { get; set; } = string.Empty;
            public string Otp { get; set; } = string.Empty;
            public DateTime ExpiresAt { get; set; }
            public bool Verified { get; set; }
        }
    }
}
