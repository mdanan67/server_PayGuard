using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using server.Data;  // ✅ add this




namespace server.model
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime? BirthDate { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Profile_image { get; set; } = string.Empty;
        public List<Guid>? Paren_ID { get; set; }
        public List<Guid>? Child_IDS { get; set; }
        public string Role { get; set; } = "parent";
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime? RefreshTokenExpiryTime { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ParentBalance? ParentBalance { get; set; }

    }

}