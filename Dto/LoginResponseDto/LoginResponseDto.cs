using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace server.Dto.LoginResponseDto
{
    public class LoginResponseDto
    {
        public Guid Id { get; set; }
        public string Role { get; set; }
        public string? FirstName { get; set; }
        public string Token { get; set; } = string.Empty;
    }
}