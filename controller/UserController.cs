using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using server.Dto;
using server.model;
using server.Dto.LoginDto;
using Microsoft.VisualBasic;
using Microsoft.AspNetCore.Identity.Data;
using server.Dto.LoginResponseDto;

using server.Data;
using server.Services;

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

            // Generate JWT token
            var token = _jwtTokenService.GenerateToken(user.Id, user.Email, user.Role);

            var response = new LoginResponseDto
            {
                Id = user.Id,
                Role = user.Role,
                FirstName = user.FirstName,
                Token = token
            };

            return Ok(response);
        }

    }

}