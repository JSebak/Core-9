using Domain.Enums;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Domain.Entities
{
    public class User
    {
        public int Id { get; private set; }
        public string Username { get; private set; }
        public string Password { get; private set; }
        public string Email { get; private set; }
        public UserRole Role { get; private set; }
        public int? ParentUserId { get; set; } = null;

        #region Navigation Properties

        [JsonIgnore]
        public virtual ICollection<User> ChildUsers { get; set; } = new List<User>();

        #endregion

        public User(string username, string password, string email, UserRole role, int? ParentUserId = null)
        {
            ValidateUsername(username);
            ValidatePassword(password);
            ValidateEmail(email);
            ValidateRole(role);
            ValidateParentId(ParentUserId);

            Username = username;
            Password = password;
            Email = email;
            Role = role;
            this.ParentUserId = ParentUserId;
        }

        public static UserRole ConvertToUserRole(string roleString)
        {
            if (Enum.TryParse(roleString, true, out UserRole role) && Enum.IsDefined(typeof(UserRole), role))
            {
                return role;
            }
            else
            {
                throw new ArgumentException($"Invalid role value: {roleString}");
            }
        }

        public void UpdatePassword(string newPassword)
        {
            ValidatePassword(newPassword);
            Password = newPassword;
        }

        public void UpdateUserName(string newUsername)
        {
            ValidateUsername(newUsername);
            Username = newUsername;
        }

        public void UpdateEmail(string newEmail)
        {
            ValidateEmail(newEmail);
            Email = newEmail;
        }

        public void ChangeRole(UserRole newRole)
        {
            ValidateRole(newRole);
            Role = newRole;
        }

        private void ValidateUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be empty", nameof(username));
        }

        private void ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty", nameof(password));

            if (password.Length < 8 ||
                !Regex.IsMatch(password, @"[A-Z]") ||
                !Regex.IsMatch(password, @"\d") ||
                !Regex.IsMatch(password, @"[@$!%*?&#]"))
            {
                throw new ArgumentException("Password must be at least 8 characters long, contain one uppercase letter, one number, and one special character.", nameof(password));
            }
        }

        private void ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty", nameof(email));

            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                throw new ArgumentException("Invalid email format", nameof(email));
            }
        }

        private void ValidateRole(UserRole role)
        {
            if (!Enum.IsDefined(typeof(UserRole), role))
                throw new ArgumentException("Invalid role", nameof(role));
        }

        private void ValidateParentId(int? parentId)
        {
            if (parentId != null)
                if (parentId <= 0) throw new ArgumentException("Invalid parent Id", nameof(parentId));
        }
    }
}
