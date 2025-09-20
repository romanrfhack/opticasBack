using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica.Domain.Entities;
using Optica.Domain.Enums;
using Optica.Infrastructure.Persistence;

namespace Optica.Api.Controllers;

public sealed record MovementCreateDto(
    string Tipo, Guid ProductoId, int Cantidad, string? Motivo,
    Guid? DesdeSucursalId, Guid? HaciaSucursalId);

[ApiController]
[Route("api/inventory/movements")]
public class InventoryMovementsController : ControllerBase
{
    private readonly AppDbContext _db;
    public InventoryMovementsController(AppDbContext db) => _db = db;

    [HttpPost]
    public async Task<IActionResult> Create(MovementCreateDto dto)
    {
        if (dto.Cantidad <= 0) return BadRequest(new { message = "Cantidad debe ser > 0" });
        if (!Enum.TryParse<TipoMovimiento>(dto.Tipo, true, out var tipo)) return BadRequest(new { message = "Tipo inválido." });

        var producto = await _db.Productos.FindAsync(dto.ProductoId);
        if (producto is null) return NotFound(new { message = "Producto no existe." });

        // Resolver sucursales segun tipo y contexto
        Guid? desde = dto.DesdeSucursalId;
        Guid? hacia = dto.HaciaSucursalId;
        var ctxSucursal = HttpContext.GetSucursalId();

        switch (tipo)
        {
            case TipoMovimiento.Entrada:
                hacia ??= ctxSucursal;
                break;
            case TipoMovimiento.Salida:
                desde ??= ctxSucursal;
                break;
            case TipoMovimiento.Traslado:
                if (desde is null) desde = ctxSucursal;
                if (hacia is null) return BadRequest(new { message = "HaciaSucursalId requerido en traslado." });
                if (desde == hacia) return BadRequest(new { message = "Sucursal origen y destino no pueden ser la misma." });
                break;
        }

        using var tx = await _db.Database.BeginTransactionAsync();

        async Task<Inventario> GetInv(Guid sucId)
        {
            var inv = await _db.Inventarios.FirstOrDefaultAsync(i => i.ProductoId == producto.Id && i.SucursalId == sucId);
            if (inv is null)
            {
                inv = new Inventario { Id = Guid.NewGuid(), ProductoId = producto.Id, SucursalId = sucId, Stock = 0, StockMin = 0 };
                _db.Inventarios.Add(inv);
                await _db.SaveChangesAsync();
            }
            return inv;
        }

        if (tipo == TipoMovimiento.Entrada)
        {
            var inv = await GetInv(hacia!.Value);
            inv.Stock += dto.Cantidad;
        }
        else if (tipo == TipoMovimiento.Salida)
        {
            var inv = await GetInv(desde!.Value);
            if (inv.Stock < dto.Cantidad) return BadRequest(new { message = "Stock insuficiente para salida." });
            inv.Stock -= dto.Cantidad;
        }
        else // Traslado
        {
            var invFrom = await GetInv(desde!.Value);
            if (invFrom.Stock < dto.Cantidad) return BadRequest(new { message = "Stock insuficiente para traslado." });
            invFrom.Stock -= dto.Cantidad;

            var invTo = await GetInv(hacia!.Value);
            invTo.Stock += dto.Cantidad;
        }

        _db.Movimientos.Add(new InventarioMovimiento
        {
            Id = Guid.NewGuid(),
            ProductoId = producto.Id,
            DesdeSucursalId = desde,
            HaciaSucursalId = hacia,
            Cantidad = dto.Cantidad,
            Tipo = tipo,
            Motivo = dto.Motivo
        });

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return NoContent();
    }
}
