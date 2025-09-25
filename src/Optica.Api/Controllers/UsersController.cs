using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica.Infrastructure.Identity;
using Optica.Infrastructure.Persistence;

namespace Optica.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly AppDbContext _db;

    public UsersController(UserManager<AppUser> userManager, RoleManager<IdentityRole<Guid>> roleManager, AppDbContext db)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
    }

    public sealed record UserItem(Guid Id, string Email, string? FullName, string? PhoneNumber, Guid SucursalId, string SucursalNombre, string[] Roles, bool LockedOut);
    public sealed record CreateUserRequest(string Email, string FullName, Guid SucursalId, string Password, string[] Roles);
    public sealed record UpdateUserRequest(string FullName, Guid SucursalId, string[] Roles);
    public sealed record ResetPasswordRequest(string NewPassword);
    public sealed record LockRequest(bool Lock);

    [HttpGet("roles")]
    public async Task<IEnumerable<string>> Roles()
        => await _roleManager.Roles.Select(r => r.Name!).OrderBy(n => n).ToListAsync();

    [HttpGet]
    public async Task<ActionResult<object>> List([FromQuery] string? query, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 20;

        var q = _userManager.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query))
        {
            var t = query.Trim().ToLower();
            q = q.Where(u => (u.Email ?? "").ToLower().Contains(t) || (u.FullName ?? "").ToLower().Contains(t));
        }

        var total = await q.CountAsync();
        var users = await q.OrderBy(u => u.Email).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var sucursales = await _db.Sucursales.ToDictionaryAsync(s => s.Id, s => s.Nombre);
        var items = new List<UserItem>(users.Count);
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            var locked = u.LockoutEnd.HasValue && u.LockoutEnd.Value.UtcDateTime > DateTime.UtcNow;
            items.Add(new UserItem(u.Id, u.Email!, u.FullName, u.PhoneNumber, u.SucursalId, sucursales.GetValueOrDefault(u.SucursalId, ""), roles.ToArray(), locked));
        }

        return Ok(new { total, items });
    }

    [HttpPost]
    public async Task<ActionResult<UserItem>> Create(CreateUserRequest req)
    {
        foreach (var r in req.Roles)
            if (!await _roleManager.RoleExistsAsync(r)) return BadRequest(new { message = $"Rol inexistente: {r}" });

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = req.Email,
            Email = req.Email,
            EmailConfirmed = true,
            FullName = req.FullName,
            SucursalId = req.SucursalId
        };

        var result = await _userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded) return BadRequest(new { message = string.Join("; ", result.Errors.Select(e => e.Description)) });

        if (req.Roles.Length > 0)
            await _userManager.AddToRolesAsync(user, req.Roles);

        var suc = await _db.Sucursales.FindAsync(req.SucursalId);
        var roles = await _userManager.GetRolesAsync(user);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, new UserItem(user.Id, user.Email!, user.FullName, user.PhoneNumber, user.SucursalId, suc?.Nombre ?? "", roles.ToArray(), false));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserItem>> GetById(Guid id)
    {
        var u = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (u is null) return NotFound();
        var roles = await _userManager.GetRolesAsync(u);
        var suc = await _db.Sucursales.FindAsync(u.SucursalId);
        var locked = u.LockoutEnd.HasValue && u.LockoutEnd.Value.UtcDateTime > DateTime.UtcNow;

        return new UserItem(u.Id, u.Email!, u.FullName, u.PhoneNumber, u.SucursalId, suc?.Nombre ?? "", roles.ToArray(), locked);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateUserRequest req)
    {
        var u = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (u is null) return NotFound();

        u.FullName = req.FullName;
        u.SucursalId = req.SucursalId;

        var res = await _userManager.UpdateAsync(u);
        if (!res.Succeeded) return BadRequest(new { message = string.Join("; ", res.Errors.Select(e => e.Description)) });

        var current = await _userManager.GetRolesAsync(u);
        var toAdd = req.Roles.Except(current).ToArray();
        var toRemove = current.Except(req.Roles).ToArray();
        if (toAdd.Length > 0) await _userManager.AddToRolesAsync(u, toAdd);
        if (toRemove.Length > 0) await _userManager.RemoveFromRolesAsync(u, toRemove);

        return NoContent();
    }

    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, ResetPasswordRequest req)
    {
        var u = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (u is null) return NotFound();
        var token = await _userManager.GeneratePasswordResetTokenAsync(u);
        var res = await _userManager.ResetPasswordAsync(u, token, req.NewPassword);
        if (!res.Succeeded) return BadRequest(new { message = string.Join("; ", res.Errors.Select(e => e.Description)) });
        return NoContent();
    }

    [HttpPost("{id:guid}/lock")]
    public async Task<IActionResult> Lock(Guid id, LockRequest req)
    {
        var u = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (u is null) return NotFound();
        if (req.Lock) u.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
        else u.LockoutEnd = null;
        await _userManager.UpdateAsync(u);
        return NoContent();
    }
}
