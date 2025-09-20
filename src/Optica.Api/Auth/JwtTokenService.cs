using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Optica.Infrastructure.Identity;

namespace Optica.Api.Auth;

public sealed class JwtTokenService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly JwtOptions _opt;
    public JwtTokenService(UserManager<AppUser> um, IOptions<JwtOptions> opt)
    {
        _userManager = um;
        _opt = opt.Value;
    }

    public async Task<(string token, DateTimeOffset expires)> CreateAccessTokenAsync(AppUser user, Guid? sucursalOverride = null)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var sucursalId = sucursalOverride ?? user.SucursalId;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new("name", user.FullName ?? user.UserName ?? ""),
            new("sucursalId", sucursalId.ToString())
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTimeOffset.UtcNow.AddMinutes(_opt.AccessTokenMinutes);

        var jwt = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires.UtcDateTime,
            signingCredentials: creds
        );
        var token = new JwtSecurityTokenHandler().WriteToken(jwt);
        return (token, expires);
    }

    public static string GenerateRefreshToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
}
