using System.ComponentModel.DataAnnotations;

namespace F1Predictions.Models.DTOs
{
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "Phone number is required")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;
    }

    public class VerifyPhoneRequestDto
    {
        [Required(ErrorMessage = "Phone number is required")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "OTP code is required")]
        public string Code { get; set; } = string.Empty;
    }

    public class RequestPasswordResetDto
    {
        [Required(ErrorMessage = "Phone number is required")]
        public string Phone { get; set; } = string.Empty;
    }

    public class VerifyResetOtpRequestDto
    {
        [Required(ErrorMessage = "Phone number is required")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "OTP code is required")]
        public string Code { get; set; } = string.Empty;
    }

    public class ResetPasswordRequestDto
    {
        [Required(ErrorMessage = "Phone number is required")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "OTP code is required")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
