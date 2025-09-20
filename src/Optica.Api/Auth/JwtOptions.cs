namespace Optica.Api.Auth;
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "OpticaApi";
    public string Audience { get; set; } = "OpticaWeb";
    public string Key { get; set; } = null!;
    public int AccessTokenMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 7;
}
