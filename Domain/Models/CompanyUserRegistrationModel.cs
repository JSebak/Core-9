using System.ComponentModel.DataAnnotations;

namespace Domain.Models
{
    public class CompanyUserRegistrationModel
    {
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public required string Email { get; set; }
        public required string UserName { get; set; }

        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#])[A-Za-z\d@$!%*?&#]{8,}$",
        ErrorMessage = "Password must be at least 8 characters long, contain one uppercase letter, one number, and one special character.")]
        public required string Password { get; set; }
        public required string Role { get; set; }
        public required int ParentId { get; set; }
    }
}
