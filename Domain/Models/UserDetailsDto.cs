using System.Text.Json.Serialization;

namespace Domain.Models
{
    public class UserDetailsDto
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        [JsonIgnore]
        public string Password { get; set; }
        public string Role { get; set; }
        public int? ParentId { get; set; }
    }
}
