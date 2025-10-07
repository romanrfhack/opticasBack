using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Optica.Domain.Dtos;
using Optica.Domain.Entities;
using Optica.Domain.Enums;
using Optica.Infrastructure.Persistence;

namespace Optica.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // autenticado (cualquier rol)
public class PacientesController : ControllerBase
{
    private readonly AppDbContext _db;
    public PacientesController(AppDbContext db) { _db = db; }

    public sealed record CreatePacienteRequest(string Nombre, int Edad, string Telefono, string Ocupacion, string? Direccion);
    public sealed record PacienteItem(Guid Id, string Nombre, int Edad, string Telefono, string Ocupacion);

    [HttpGet("search")]
    public async Task<IEnumerable<PacienteItem>> Search([FromQuery] string term)
    {
        if (string.IsNullOrWhiteSpace(term)) return [];
        var t = term.Trim().ToLower();

        return await _db.Pacientes
            .Where(p => p.Nombre.ToLower().Contains(t) || p.Telefono.Contains(term))
            .OrderBy(p => p.Nombre).Take(20)
            .Select(p => new PacienteItem(p.Id, p.Nombre, p.Edad, p.Telefono, p.Ocupacion))
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<PacienteItem>> Create(CreatePacienteRequest req)
    {
        var sucursalId = Guid.Parse(User.FindFirst("sucursalId")!.Value);

        var p = new Paciente
        {
            Id = Guid.NewGuid(),
            Nombre = req.Nombre,
            Edad = req.Edad,
            Telefono = req.Telefono,
            Ocupacion = req.Ocupacion,
            Direccion = req.Direccion,
            SucursalIdAlta = sucursalId
        };

        _db.Pacientes.Add(p);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = p.Id },
            new PacienteItem(p.Id, p.Nombre, p.Edad, p.Telefono, p.Ocupacion));
    }

    [HttpGet("query")]
    public async Task<PagedResult<PacienteGridItemDto>> Query(
    [FromQuery] string? term,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 20 : pageSize;

        var q = _db.Pacientes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(term))
        {
            term = term.Trim();
            q = q.Where(p =>
                (p.Nombre != null && p.Nombre.Contains(term)) ||
                (p.Telefono != null && p.Telefono.Contains(term)));
        }

        var total = await q.CountAsync();

        var items = await q
            .OrderBy(p => p.Nombre)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PacienteGridItemDto
            {
                Id = p.Id,
                Nombre = p.Nombre!,
                Edad = p.Edad,
                Telefono = p.Telefono,
                Ocupacion = p.Ocupacion,

                UltimaVisitaFecha = p.Visitas.OrderByDescending(v => v.Fecha).Select(v => (DateTime?)v.Fecha).FirstOrDefault(),
                UltimaVisitaEstado = p.Visitas.OrderByDescending(v => v.Fecha).Select(v => v.Estado.ToString()).FirstOrDefault(),
                UltimaVisitaTotal = p.Visitas.OrderByDescending(v => v.Fecha).Select(v => v.Total).FirstOrDefault(),
                UltimaVisitaACuenta = p.Visitas.OrderByDescending(v => v.Fecha)
                    .Select(v => v.Pagos.Sum(pg => (decimal?)pg.Monto) ?? 0m).FirstOrDefault(),
                UltimaVisitaResta = p.Visitas.OrderByDescending(v => v.Fecha)
                    .Select(v => (v.Total ?? 0m) - (v.Pagos.Sum(pg => (decimal?)pg.Monto) ?? 0m)).FirstOrDefault(),

                UltimoPagoFecha = p.Visitas.SelectMany(v => v.Pagos)
                    .OrderByDescending(pg => pg.Fecha).Select(pg => (DateTime?)pg.Fecha).FirstOrDefault(),
                UltimoPagoMonto = p.Visitas.SelectMany(v => v.Pagos)
                    .OrderByDescending(pg => pg.Fecha).Select(pg => (decimal?)pg.Monto).FirstOrDefault(),

                TieneOrdenPendiente =
                    p.Visitas.OrderByDescending(v => v.Fecha)
                        .Select(v => v.Estado == EstadoHistoria.EnLaboratorio ||
                                      ((v.Total ?? 0m) - (v.Pagos.Sum(pg => (decimal?)pg.Monto) ?? 0m)) > 0m)
                        .FirstOrDefault()
            })
            .ToListAsync();

        return new PagedResult<PacienteGridItemDto>
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        };
    }

    [HttpGet("{id:guid}/grid")]
    public async Task<ActionResult<PacienteGridItemDto>> GetGridById(Guid id)
    {
        var dto = await _db.Pacientes
            .Where(p => p.Id == id)
            .Select(p => new PacienteGridItemDto
            {
                Id = p.Id,
                Nombre = p.Nombre!,
                Edad = p.Edad,
                Telefono = p.Telefono,
                Ocupacion = p.Ocupacion,

                UltimaVisitaFecha = p.Visitas.OrderByDescending(v => v.Fecha).Select(v => (DateTime?)v.Fecha).FirstOrDefault(),
                UltimaVisitaEstado = p.Visitas.OrderByDescending(v => v.Fecha).Select(v => v.Estado.ToString()).FirstOrDefault(),
                UltimaVisitaTotal = p.Visitas.OrderByDescending(v => v.Fecha).Select(v => v.Total).FirstOrDefault(),
                UltimaVisitaACuenta = p.Visitas.OrderByDescending(v => v.Fecha)
                    .Select(v => v.Pagos.Sum(pg => (decimal?)pg.Monto) ?? 0m).FirstOrDefault(),
                UltimaVisitaResta = p.Visitas.OrderByDescending(v => v.Fecha)
                    .Select(v => (v.Total ?? 0m) - (v.Pagos.Sum(pg => (decimal?)pg.Monto) ?? 0m)).FirstOrDefault(),

                UltimoPagoFecha = p.Visitas.SelectMany(v => v.Pagos)
                    .OrderByDescending(pg => pg.Fecha).Select(pg => (DateTime?)pg.Fecha).FirstOrDefault(),
                UltimoPagoMonto = p.Visitas.SelectMany(v => v.Pagos)
                    .OrderByDescending(pg => pg.Fecha).Select(pg => (decimal?)pg.Monto).FirstOrDefault(),

                TieneOrdenPendiente =
                    p.Visitas.OrderByDescending(v => v.Fecha)
                        .Select(v => v.Estado == EstadoHistoria.EnLaboratorio ||
                                     ((v.Total ?? 0m) - (v.Pagos.Sum(pg => (decimal?)pg.Monto) ?? 0m)) > 0m)
                        .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (dto is null) return NotFound();
        return dto;
    }

    [HttpGet("grid")]
    public async Task<PagedResult<PacienteGridItemDto>> Grid([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 20 : pageSize;

        var baseQ = _db.Pacientes.AsNoTracking();
        var total = await baseQ.CountAsync();

        var items = await baseQ
            .OrderBy(p => p.Nombre)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new PacienteGridItemDto
            {
                Id = p.Id,
                Nombre = p.Nombre!,
                Edad = p.Edad,
                Telefono = p.Telefono,
                Ocupacion = p.Ocupacion,

                UltimaVisitaFecha = p.Visitas.OrderByDescending(v => v.Fecha).Select(v => (DateTime?)v.Fecha).FirstOrDefault(),
                UltimaVisitaEstado = p.Visitas.OrderByDescending(v => v.Fecha).Select(v => v.Estado.ToString()).FirstOrDefault(),
                UltimaVisitaTotal = p.Visitas.OrderByDescending(v => v.Fecha).Select(v => v.Total).FirstOrDefault(),
                UltimaVisitaACuenta = p.Visitas.OrderByDescending(v => v.Fecha)
                    .Select(v => v.Pagos.Sum(pg => (decimal?)pg.Monto) ?? 0m).FirstOrDefault(),
                UltimaVisitaResta = p.Visitas.OrderByDescending(v => v.Fecha)
                    .Select(v => (v.Total ?? 0m) - (v.Pagos.Sum(pg => (decimal?)pg.Monto) ?? 0m)).FirstOrDefault(),

                UltimoPagoFecha = p.Visitas.SelectMany(v => v.Pagos)
                    .OrderByDescending(pg => pg.Fecha).Select(pg => (DateTime?)pg.Fecha).FirstOrDefault(),
                UltimoPagoMonto = p.Visitas.SelectMany(v => v.Pagos)
                    .OrderByDescending(pg => pg.Fecha).Select(pg => (decimal?)pg.Monto).FirstOrDefault(),

                TieneOrdenPendiente =
                    p.Visitas.OrderByDescending(v => v.Fecha)
                        .Select(v => v.Estado == EstadoHistoria.EnLaboratorio ||
                                     ((v.Total ?? 0m) - (v.Pagos.Sum(pg => (decimal?)pg.Monto) ?? 0m)) > 0m)
                        .FirstOrDefault()
            })
            .ToListAsync();

        return new PagedResult<PacienteGridItemDto>
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        };
    }

    [HttpGet("{pacienteId:guid}/historial")]
    public async Task<PagedResult<VisitaRowDto>> Historial(
    Guid pacienteId,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] EstadoHistoria? estado = null,
    [FromQuery] DateTime? from = null,
    [FromQuery] DateTime? to = null,
    [FromQuery] bool soloPendientes = false)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 20 : pageSize;

        var q = _db.Visitas.AsNoTracking().Where(v => v.PacienteId == pacienteId);

        if (estado.HasValue) q = q.Where(v => v.Estado == estado.Value);
        if (from.HasValue) q = q.Where(v => v.Fecha >= from.Value);
        if (to.HasValue) q = q.Where(v => v.Fecha <= to.Value);

        if (soloPendientes)
        {
            q = q.Where(v =>
                v.Estado == EstadoHistoria.EnLaboratorio ||
                ((v.Total ?? 0m) - (v.Pagos.Sum(p => (decimal?)p.Monto) ?? 0m)) > 0m
            );
        }

        var total = await q.CountAsync();

        var items = await q
            .OrderByDescending(v => v.Fecha)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(v => new VisitaRowDto
            {
                Id = v.Id,
                Fecha = v.Fecha,
                Estado = v.Estado.ToString(),
                Total = v.Total,
                ACuenta = v.Pagos.Sum(p => (decimal?)p.Monto) ?? 0m,
                Resta = (v.Total ?? 0m) - (v.Pagos.Sum(p => (decimal?)p.Monto) ?? 0m),

                UltimoPagoFecha = v.Pagos.OrderByDescending(p => p.Fecha)
                                         .Select(p => (DateTime?)p.Fecha).FirstOrDefault(),
                UltimoPagoMonto = v.Pagos.OrderByDescending(p => p.Fecha)
                                         .Select(p => (decimal?)p.Monto).FirstOrDefault(),

                FechaEstimadaEntrega = v.FechaEstimadaEntrega,
                FechaRecibidaSucursal = v.FechaRecibidoSucursal,
                FechaEntregadaCliente = v.FechaEntregaCliente
            })
            .ToListAsync();

        return new PagedResult<VisitaRowDto>
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        };
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Paciente>> GetById(Guid id)
        => await _db.Pacientes.FindAsync(id) is { } p ? Ok(p) : NotFound();
}
