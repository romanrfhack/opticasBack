using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica.Domain.Dtos;
using Optica.Domain.Entities;
using Optica.Infrastructure.Identity;
using Optica.Infrastructure.Persistence;

namespace Optica.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PasswordController : ControllerBase
    {
        private readonly UserManager<AppUser> _users;
        private readonly AppDbContext _db;
        private readonly ILogger<PasswordController> _logger;

        public PasswordController(UserManager<AppUser> users, AppDbContext db, ILogger<PasswordController> logger)
        {
            _users = users; _db = db; _logger = logger;
        }

        // POST /api/password/forgot
        [HttpPost("forgot")]
        [AllowAnonymous]
        public async Task<IActionResult> Forgot([FromBody] ForgotPasswordRequest req)
        {
            var user = await _users.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
            // No revelamos si el correo existe:
            if (user is null) return Accepted(new { message = "Si el correo existe, se enviará un enlace de recuperación." });

            var token = await _users.GeneratePasswordResetTokenAsync(user);
            // Aquí normalmente enviarías un correo; por ahora se loguea.
            _logger.LogInformation("Password reset token for {Email}: {Token}", req.Email, token);

            return Accepted(new { message = "Se ha enviado un enlace de recuperación al correo registrado." });
        }

        // POST /api/password/reset
        [HttpPost("reset")]
        [AllowAnonymous]
        public async Task<IActionResult> Reset([FromBody] ResetPasswordRequest req)
        {
            var user = await _users.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
            if (user is null) return BadRequest(new { message = "Usuario no encontrado." });

            var result = await _users.ResetPasswordAsync(user, req.Token, req.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                return BadRequest(new { message = errors });
            }
            return NoContent();
        }
    }
}
