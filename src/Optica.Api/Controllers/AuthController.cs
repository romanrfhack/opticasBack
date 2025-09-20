using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica.Api.Auth;
using Optica.Domain.Entities;
using Optica.Infrastructure.Identity;
using Optica.Infrastructure.Persistence;

namespace Optica.Api.Controllers;

public sealed record LoginRequest(string Email, string Password);
public sealed record TokenResponse(string AccessToken, string RefreshToken, long ExpiresInSeconds, object User);
public sealed record RefreshRequest(string RefreshToken);
public sealed record UpdateProfileRequest(string FullName, string? PhoneNumber);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record SwitchBranchRequest(Guid TargetSucursalId);

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
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest req,
        [FromServices] UserManager<AppUser> _userManager)
    {
        //var userId = User?.FindFirst("sub")?.Value;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return Unauthorized();

        user.FullName = req.FullName;
        if (!string.IsNullOrWhiteSpace(req.PhoneNumber))
            user.PhoneNumber = req.PhoneNumber;

        var res = await _userManager.UpdateAsync(user);
        if (!res.Succeeded)
            return BadRequest(new { message = string.Join("; ", res.Errors.Select(e => e.Description)) });

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new { id = user.Id, name = user.FullName ?? user.UserName, email = user.Email, sucursalId = user.SucursalId, roles });
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req,
        [FromServices] UserManager<AppUser> _userManager)
    {
        //var userId = User?.FindFirst("sub")?.Value;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return Unauthorized();

        var res = await _userManager.ChangePasswordAsync(user, req.CurrentPassword, req.NewPassword);
        if (!res.Succeeded)
            return BadRequest(new { message = string.Join("; ", res.Errors.Select(e => e.Description)) });

        return NoContent();
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
