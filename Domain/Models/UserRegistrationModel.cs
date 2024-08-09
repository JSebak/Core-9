using System.ComponentModel.DataAnnotations;

namespace Domain.Models
{
    public class UserRegistrationModel
    {
        [Required]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; }
        [Required]
        public string UserName { get; set; }
        [Required]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#])[A-Za-z\d@$!%*?&#]{8,}$",
        ErrorMessage = "Password must be at least 8 characters long, contain one uppercase letter, one number, and one special character.")]
        public string Password { get; set; }
        [Required]
        public string Role { get; set; }
    }
}
