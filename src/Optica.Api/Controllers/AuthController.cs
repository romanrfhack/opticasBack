using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Optica.Api.Auth;
using Optica.Domain.Dtos;
using Optica.Domain.Entities;
using Optica.Infrastructure.Identity;
using Optica.Infrastructure.Persistence;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;

namespace Optica.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly JwtTokenService _tokens;
    private readonly AppDbContext _db;

    public AuthController(UserManager<AppUser> um, SignInManager<AppUser> sm, JwtTokenService t, AppDbContext db)
    {
        _userManager = um;
        _signInManager = sm;
        _tokens = t;
        _db = db;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponse>> Login(LoginRequest req)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
        if (user is null) return Unauthorized();

        var ok = await _signInManager.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: false);
        if (!ok.Succeeded) return Unauthorized();

        var (access, exp) = await _tokens.CreateAccessTokenAsync(user);
        var refresh = JwtTokenService.GenerateRefreshToken();
        _db.RefreshTokens.Add(new RefreshToken { Id = Guid.NewGuid(), UserId = user.Id, Token = refresh, ExpiresAt = DateTimeOffset.UtcNow.AddDays(7) });
        await _db.SaveChangesAsync();

        var roles = await _userManager.GetRolesAsync(user);
        return new TokenResponse(access, refresh, (long)(exp - DateTimeOffset.UtcNow).TotalSeconds,
            new { id = user.Id, name = user.FullName ?? user.UserName, email = user.Email, sucursalId = user.SucursalId, roles });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponse>> Refresh(RefreshRequest req)
    {
        var rt = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == req.RefreshToken);
        if (rt is null || rt.RevokedAt != null || rt.ExpiresAt < DateTimeOffset.UtcNow) return Unauthorized();

        var user = await _userManager.FindByIdAsync(rt.UserId.ToString());
        if (user is null) return Unauthorized();

        rt.RevokedAt = DateTimeOffset.UtcNow;
        var newToken = JwtTokenService.GenerateRefreshToken();
        _db.RefreshTokens.Add(new RefreshToken { Id = Guid.NewGuid(), UserId = user.Id, Token = newToken, ExpiresAt = DateTimeOffset.UtcNow.AddDays(7) });

        var (access, exp) = await _tokens.CreateAccessTokenAsync(user);
        await _db.SaveChangesAsync();

        var roles = await _userManager.GetRolesAsync(user);
        return new TokenResponse(access, newToken, (long)(exp - DateTimeOffset.UtcNow).TotalSeconds,
            new { id = user.Id, name = user.FullName ?? user.UserName, email = user.Email, sucursalId = user.SucursalId, roles });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequest req)
    {
        var rt = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == req.RefreshToken);
        if (rt != null) { rt.RevokedAt = DateTimeOffset.UtcNow; await _db.SaveChangesAsync(); }
        return NoContent();
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateMe(
    [FromBody] UpdateProfileRequest req,
    [FromServices] UserManager<AppUser> userManager,
    [FromServices] IEmailSender emailSender) // Inyectar servicio de email
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                       ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out Guid userGuid))
                return Unauthorized("No se pudo identificar al usuario.");

            var user = await userManager.FindByIdAsync(userGuid.ToString());
            if (user == null)
                return Unauthorized("Usuario no encontrado.");

            bool hasChanges = false;
            bool emailChanged = false;
            string? oldEmail = null;

            // Verificar si el nombre cambió
            if (user.FullName != req.FullName)
            {
                user.FullName = req.FullName;
                hasChanges = true;
            }

            // Verificar si el email cambió
            if (!string.IsNullOrEmpty(req.email) && user.Email != req.email)
            {
                var existingUser = await userManager.FindByEmailAsync(req.email);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    return BadRequest(new
                    {
                        message = "El email ya está en uso por otro usuario."
                    });
                }

                oldEmail = user.Email; // Guardar el email antiguo
                user.Email = req.email;
                user.EmailConfirmed = false; // Marcar como no confirmado
                user.UserName = req.email; // Actualizar también el username si es necesario
                hasChanges = true;
                emailChanged = true;
            }

            // Verificar si el teléfono cambió
            if (req.PhoneNumber != user.PhoneNumber)
            {
                user.PhoneNumber = req.PhoneNumber;
                hasChanges = true;
            }

            EmailChangeResult emailChangeResult = new();

            // Solo actualizar si hay cambios reales
            if (hasChanges)
            {
                var result = await userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description);
                    return BadRequest(new
                    {
                        message = "Error al actualizar el perfil.",
                        errors = errors
                    });
                }

                // Si el email cambió, enviar email de verificación
                if (emailChanged && !string.IsNullOrEmpty(oldEmail))
                {
                    emailChangeResult = await SendEmailVerification(user, emailSender);
                }
            }

            var roles = await userManager.GetRolesAsync(user);

            return Ok(new
            {
                id = user.Id,
                name = user.FullName ?? user.UserName,
                email = user.Email,
                sucursalId = user.SucursalId,
                roles,
                requiresEmailConfirmation = emailChanged,
                emailVerificationSent = emailChangeResult.Success,
                emailVerificationMessage = emailChangeResult.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Error interno del servidor al actualizar el perfil."
            });
        }
    }

    private async Task<EmailChangeResult> SendEmailVerification(AppUser user, IEmailSender emailSender)
    {
        try
        {
            // Generar token de confirmación
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            // Crear enlace de confirmación (ajusta la URL según tu frontend)
            var confirmationLink = $"https://tudominio.com/confirm-email?userId={user.Id}&token={WebUtility.UrlEncode(token)}";

            // Crear el contenido del email
            var subject = "Confirma tu nuevo email";
            var message = $@"
            <h2>Confirma tu nuevo email</h2>
            <p>Hola {user.FullName},</p>
            <p>Has solicitado cambiar tu email a {user.Email}. Para completar este proceso, por favor confirma tu nuevo email haciendo clic en el siguiente enlace:</p>
            <p><a href='{confirmationLink}' style='background-color: #06b6d4; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Confirmar Email</a></p>
            <p>Si no solicitaste este cambio, por favor ignora este mensaje.</p>
            <br>
            <p>Saludos,<br>El equipo de tu aplicación</p>
        ";

            // Enviar email
            await emailSender.SendEmailAsync(user.Email, subject, message);

            return new EmailChangeResult { Success = true, Message = "Email de verificación enviado" };
        }
        catch (Exception ex)
        {
            return new EmailChangeResult { Success = false, Message = "Error al enviar email de verificación" };
        }
    }

    public class EmailChangeResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest req,
        [FromServices] UserManager<AppUser> userManager)
    {
        // Validar el modelo de entrada
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            // Obtener el ID del usuario desde los claims
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                       ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out Guid userGuid))
                return Unauthorized("No se pudo identificar al usuario.");

            // Buscar el usuario
            var user = await userManager.FindByIdAsync(userGuid.ToString());
            if (user == null)
                return Unauthorized("Usuario no encontrado.");

            // Validar que la contraseña actual sea correcta
            var isCurrentPasswordValid = await userManager.CheckPasswordAsync(user, req.CurrentPassword);
            if (!isCurrentPasswordValid)
                return BadRequest(new { message = "La contraseña actual es incorrecta." });

            // Cambiar la contraseña
            var result = await userManager.ChangePasswordAsync(user, req.CurrentPassword, req.NewPassword);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                return BadRequest(new
                {
                    message = "Error al cambiar la contraseña.",
                    errors = errors
                });
            }

            return Ok(new { message = "Contraseña cambiada exitosamente." });
        }
        catch (Exception ex)
        {
            // Log the exception (deberías tener un servicio de logging aquí)
            // _logger.LogError(ex, "Error cambiando contraseña para el usuario {UserId}", userId);

            return StatusCode(500, new
            {
                message = "Error interno del servidor al cambiar la contraseña."
            });
        }
    }

    [HttpPost("switch-branch")]
    [Authorize(Roles = "Admin")] // ajusta si quieres permitir a otros roles
    public async Task<ActionResult<TokenResponse>> SwitchBranch([FromBody] SwitchBranchRequest req)
    {
        var suc = await _db.Sucursales.AsNoTracking().FirstOrDefaultAsync(s => s.Id == req.TargetSucursalId);
        if (suc is null) return NotFound(new { message = "Sucursal no encontrada" });

        //var userId = User?.FindFirst("sub")?.Value;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return Unauthorized();

        // Emite nuevo access token con sucursal override + refresh token nuevo
        var (access, exp) = await _tokens.CreateAccessTokenAsync(user, req.TargetSucursalId);
        var refresh = JwtTokenService.GenerateRefreshToken();

        _db.RefreshTokens.Add(new Optica.Domain.Entities.RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refresh,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        });
        await _db.SaveChangesAsync();

        var roles = await _userManager.GetRolesAsync(user);

        return new TokenResponse(access, refresh, (long)(exp - DateTimeOffset.UtcNow).TotalSeconds,
            new { id = user.Id, name = user.FullName ?? user.UserName, email = user.Email, sucursalId = user.SucursalId, roles });
    }
}
