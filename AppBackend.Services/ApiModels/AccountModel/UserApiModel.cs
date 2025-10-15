using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels
{
    public class UserCreateRequest
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(255, ErrorMessage = "Full name cannot exceed 255 characters.")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "EmailServices is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(255, ErrorMessage = "EmailServices cannot exceed 255 characters.")]
        public string Email { get; set; } = null!;

        [Phone(ErrorMessage = "Invalid phone number format.")]
        [StringLength(15, ErrorMessage = "Phone number cannot exceed 15 digits.")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "RoleId is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "RoleId must be greater than 0.")]
        public int RoleId { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        public string PasswordHash { get; set; } = null!;
    }

    public class UserUpdateRequest
    {
        [Required(ErrorMessage = "UserId is required.")]
        public int UserId { get; set; }

        [StringLength(255, ErrorMessage = "Full name cannot exceed 255 characters.")]
        public string? FullName { get; set; }

        [Phone(ErrorMessage = "Invalid phone number format.")]
        [StringLength(15, ErrorMessage = "Phone number cannot exceed 15 digits.")]
        public string? Phone { get; set; }

        [Url(ErrorMessage = "AvatarUrl must be a valid URL.")]
        public string? AvatarUrl { get; set; }
    }
    public class UserDto
    {
        public int UserId { get; set; }
        public string? FullName { get; set; }
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public int? RoleId { get; set; }
        public string? AvatarUrl { get; set; }
    }
}