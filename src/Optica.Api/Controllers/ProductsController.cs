using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica.Application.Productos.Dtos;
using Optica.Domain.Entities;
using Optica.Domain.Enums;
using Optica.Infrastructure.Persistence;

namespace Optica.Api.Controllers;

public sealed record ProductDto(Guid Id, string Sku, string Nombre, string Categoria, bool Activo);
public sealed record ProductCreateDto(string Sku, string Nombre, string Categoria);
public sealed record ProductUpdateDto(string Sku, string Nombre, string Categoria, bool Activo);

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ProductsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> Get([FromQuery] string? q = null)
    {
        var term = (q ?? "").Trim();
        var like = $"%{term}%";
        var query = _db.Productos.AsQueryable();
        if (!string.IsNullOrWhiteSpace(term))
            query = query.Where(p => EF.Functions.Like(p.Sku, like) || EF.Functions.Like(p.Nombre, like));
        var list = await query.OrderBy(p => p.Nombre)
            .Select(p => new ProductDto(p.Id, p.Sku, p.Nombre, p.Categoria.ToString(), p.Activo))
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetById(Guid id)
    {
        var p = await _db.Productos.FindAsync(id);
        return p is null ? NotFound() : new ProductDto(p.Id, p.Sku, p.Nombre, p.Categoria.ToString(), p.Activo);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(ProductCreateDto dto)
    {
        if (await _db.Productos.AnyAsync(x => x.Sku == dto.Sku))
            return Conflict(new { message = "SKU duplicado." });

        if (!Enum.TryParse<CategoriaProducto>(dto.Categoria, true, out var cat))
            return BadRequest(new { message = "Categoría inválida." });

        var p = new Producto { Id = Guid.NewGuid(), Sku = dto.Sku.Trim(), Nombre = dto.Nombre.Trim(), Categoria = cat, Activo = true };
        _db.Productos.Add(p);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = p.Id }, new ProductDto(p.Id, p.Sku, p.Nombre, p.Categoria.ToString(), p.Activo));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductDto>> Update(Guid id, ProductUpdateDto dto)
    {
        var p = await _db.Productos.FindAsync(id);
        if (p is null) return NotFound();

        if (p.Sku != dto.Sku && await _db.Productos.AnyAsync(x => x.Sku == dto.Sku))
            return Conflict(new { message = "SKU duplicado." });

        if (!Enum.TryParse<CategoriaProducto>(dto.Categoria, true, out var cat))
            return BadRequest(new { message = "Categoría inválida." });

        p.Sku = dto.Sku.Trim();
        p.Nombre = dto.Nombre.Trim();
        p.Categoria = cat;
        p.Activo = dto.Activo;
        await _db.SaveChangesAsync();

        return new ProductDto(p.Id, p.Sku, p.Nombre, p.Categoria.ToString(), p.Activo);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var used = await _db.Inventarios.AnyAsync(i => i.ProductoId == id);
        if (used) return Conflict(new { message = "No se puede borrar: el producto tiene inventario." });

        var p = await _db.Productos.FindAsync(id);
        if (p is null) return NotFound();

        _db.Productos.Remove(p);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("armazones")]
    public async Task<ActionResult<IEnumerable<ProductArmazonDto>>> GetArmazones([FromQuery] string? q = null)
    {
        var sucursalId = Guid.Parse(User.FindFirst("sucursalId")!.Value);
        var term = (q ?? "").Trim();
        var like = $"%{term}%";

        // Consulta para obtener productos con información de inventario
        var query = _db.Productos
            .Where(p => p.Categoria == CategoriaProducto.Armazon && p.Activo)
            .Select(p => new
            {
                Producto = p,
                // Stock en sucursal activa
                StockSucursalActiva = p.Inventarios
                    .Where(i => i.SucursalId == sucursalId)
                    .Select(i => i.Stock)
                    .FirstOrDefault(),
                // Todas las sucursales con stock
                SucursalesConStock = p.Inventarios
                    .Where(i => i.Stock > 0)
                    .Select(i => new SucursalStockDto
                    {
                        SucursalId = i.SucursalId,
                        NombreSucursal = i.Sucursal.Nombre,
                        Stock = i.Stock
                    })
                    .ToList()
            });

        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(x =>
                EF.Functions.Like(x.Producto.Sku, like) ||
                EF.Functions.Like(x.Producto.Nombre, like));
        }

        var result = await query.OrderBy(x => x.Producto.Nombre).ToListAsync();

        // Mapear a DTO
        var list = result.Select(x => new ProductArmazonDto(
            x.Producto.Id,
            x.Producto.Sku,
            x.Producto.Nombre,
            x.Producto.Categoria.ToString(),
            x.Producto.Activo,
            x.StockSucursalActiva,  // Stock en sucursal activa
            x.StockSucursalActiva > 0  // EnSucursalActiva
        )
        {
            SucursalesConStock = x.SucursalesConStock
        }).ToList();

        return Ok(list);
    }
}
