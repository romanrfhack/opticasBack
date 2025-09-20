namespace Microsoft.AspNetCore.Http;
public static class HttpContextSucursalExtensions
{
    public static Guid GetSucursalId(this HttpContext http)
    {
        var claim = http?.User?.FindFirst("sucursalId")?.Value;
        if(Guid.TryParse(claim, out var c)) return c;
        var header = http?.Request?.Headers?["X-Sucursal-Id"].FirstOrDefault();
        if(Guid.TryParse(header, out var h)) return h;
        return Guid.Parse("11111111-1111-1111-1111-111111111111"); // fallback dev
    }
}
