﻿namespace Domain.Models
{
    public class UserUpdateModel
    {
        public string UserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;
        public int? ParentParentUserId { get; set; }
        public bool Active { get; set; }
    }
}
