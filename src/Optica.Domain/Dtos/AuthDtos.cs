using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Optica.Domain.Dtos
{
    public sealed record LoginRequest(string Email, string Password);
    public sealed record TokenResponse(string AccessToken, string RefreshToken, long ExpiresInSeconds, object User);
    public sealed record RefreshRequest(string RefreshToken);
    public sealed record UpdateProfileRequest(string FullName, string? PhoneNumber);
    public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
    public sealed record SwitchBranchRequest(Guid TargetSucursalId);
    public sealed class ForgotPasswordRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = default!;
    }

    public sealed class CustomResetPasswordRequest
    {
        [Required] public string Email { get; set; } = default!;
        [Required] public string Token { get; set; } = default!;
        [Required] public string NewPassword { get; set; } = default!;
    }

    public sealed class SupportCreateRequest
    {
        public string? Email { get; set; }
        public string Asunto { get; set; } = default!;
        public string Mensaje { get; set; } = default!;
    }
}
