using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica.Infrastructure.Persistence;
using Optica.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Optica.Api.Controllers;

public sealed record InventorySearchItemDto(
    Guid ProductId, string Sku, string Nombre, string Categoria,
    Guid SucursalId, string SucursalNombre, int Stock, int StockMin,
    bool Shared, bool BajoMin);

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly AppDbContext _db;
    public InventoryController(AppDbContext db) => _db = db;

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<InventorySearchItemDto>>> Search([FromQuery] string? q)
    {
        var sucursalId = HttpContext.GetSucursalId();
        var term = (q ?? "").Trim();
        var like = $"%{term}%";
        var query = from inv in _db.Inventarios
                    join p in _db.Productos on inv.ProductoId equals p.Id
                    join s in _db.Sucursales on inv.SucursalId equals s.Id
                    where p.Activo
                    select new { inv, p, s };
        if(!string.IsNullOrEmpty(term))
            query = query.Where(x => EF.Functions.Like(x.p.Sku, like) || EF.Functions.Like(x.p.Nombre, like));

        var visibles = query.Where(x => x.p.Categoria == CategoriaProducto.Armazon || x.inv.SucursalId == sucursalId);

        var list = await visibles
            .OrderBy(x => x.p.Nombre)
            .Select(x => new InventorySearchItemDto(
                x.p.Id, x.p.Sku, x.p.Nombre, x.p.Categoria.ToString(),
                x.s.Id, x.s.Nombre, x.inv.Stock, x.inv.StockMin,
                x.p.Categoria == CategoriaProducto.Armazon,
                x.inv.StockMin > 0 && x.inv.Stock <= x.inv.StockMin))
            .ToListAsync();
        return Ok(list);
    }
}
