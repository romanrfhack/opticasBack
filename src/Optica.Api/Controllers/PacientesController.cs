using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Optica.Application.Pacientes.Dtos;
using Optica.Application.Pacientes.Selectors;
using Optica.Domain.Dtos;
using Optica.Domain.Entities;
using Optica.Domain.Enums;
using Optica.Infrastructure.Persistence;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Optica.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PacientesController : ControllerBase
{
    private readonly AppDbContext _db;
    public PacientesController(AppDbContext db) { _db = db; }

    // --- Requests/DTOs locales (ligeros para endpoints específicos) ---
    public sealed record CreatePacienteRequest(string Nombre, int Edad, string Telefono, string Ocupacion, string? Direccion);
    public sealed record SearchPacienteItem(Guid Id, string Nombre, string Telefono, int Edad, string Ocupacion);

    // ------------------------------------------------------------------
    // SEARCH (ligero) - usa campos normalizados para coincidencias
    // ------------------------------------------------------------------
    [HttpGet("search")]
    public async Task<IEnumerable<SearchPacienteItem>> Search([FromQuery] string term, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(term)) return Array.Empty<SearchPacienteItem>();

        var t = term.Trim().ToUpper();

        return await _db.Pacientes
            .AsNoTracking()
            .Where(p =>
                (p.NombreNormalized != null && p.NombreNormalized.Contains(t)) ||
                (p.TelefonoNormalized != null && p.TelefonoNormalized.Contains(t)))
            .OrderBy(p => p.Nombre)
            .Take(20)
            .Select(p => new SearchPacienteItem(p.Id, p.Nombre, p.Telefono, p.Edad, p.Ocupacion))
            .ToListAsync(ct);
    }

    // ------------------------------------------------------------------
    // CREATE - valida duplicado y devuelve DTO completo (selector)
    // ------------------------------------------------------------------
    [HttpPost]
    public async Task<ActionResult<PacienteItem>> Create(CreatePacienteRequest req, CancellationToken ct)
    {
        var sucursalId = Guid.Parse(User.FindFirst("sucursalId")!.Value);
        
        string? GetClaim(params string[] types)
            => types.Select(t => User.FindFirst(t)?.Value)
                .FirstOrDefault(v => !string.IsNullOrEmpty(v));

        var userIdStr = GetClaim(JwtRegisteredClaimNames.Sub, ClaimTypes.NameIdentifier, "sub");
        var userEmail = GetClaim(JwtRegisteredClaimNames.Email, ClaimTypes.Email, "email");
        var userName = GetClaim("name", ClaimTypes.Name, JwtRegisteredClaimNames.UniqueName) ?? User.Identity?.Name;


        // Validación rápida (además del índice único en BD)
        var nombreNorm = (req.Nombre ?? string.Empty).Trim().ToUpper();
        var telNorm = (req.Telefono ?? string.Empty).Trim();

        var yaExiste = await _db.Pacientes.AnyAsync(p =>
            p.NombreNormalized == nombreNorm && p.TelefonoNormalized == telNorm, ct);

        if (yaExiste)
            return Conflict(new { message = "Ya existe un paciente con el mismo nombre y teléfono." });

        var paciente = new Paciente
        {
            Id = Guid.NewGuid(),
            Nombre = req.Nombre,
            Edad = req.Edad,
            Telefono = req.Telefono,
            Ocupacion = req.Ocupacion,
            Direccion = req.Direccion,
            SucursalIdAlta = sucursalId,
            CreadoPorUsuarioId = Guid.TryParse(userIdStr, out var uid) ? uid : null,
            CreadoPorNombre = userName,
            CreadoPorEmail = userEmail
            // FechaRegistro -> GETUTCDATE() en SQL
        };

        _db.Pacientes.Add(paciente);
        await _db.SaveChangesAsync(ct);

        // Proyección consistente a DTO completo
        var dto = await _db.Pacientes
            .AsNoTracking()
            .Where(p => p.Id == paciente.Id)
            .Select(PacienteSelectors.ToItem)   // incluye SucursalNombre y CreadoPor
            .FirstAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    // ------------------------------------------------------------------
    // QUERY (grid con filtros) - mantiene tu DTO actual
    // ------------------------------------------------------------------
    [HttpGet("query")]
    public async Task<PagedResult<PacienteGridItemDto>> Query(
        [FromQuery] string? term,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 20 : pageSize;

        var q = _db.Pacientes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(term))
        {
            var t = term.Trim().ToUpper();
            q = q.Where(p =>
                (p.NombreNormalized != null && p.NombreNormalized.Contains(t)) ||
                (p.TelefonoNormalized != null && p.TelefonoNormalized.Contains(t)));
        }

        var total = await q.CountAsync(ct);

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
            .ToListAsync(ct);

        return new PagedResult<PacienteGridItemDto>
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        };
    }

    // ------------------------------------------------------------------
    // GRID BY ID (mismo DTO de grid)
    // ------------------------------------------------------------------
    [HttpGet("{id:guid}/grid")]
    public async Task<ActionResult<PacienteGridItemDto>> GetGridById(Guid id, CancellationToken ct)
    {
        var dto = await _db.Pacientes
            .AsNoTracking()
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
            .FirstOrDefaultAsync(ct);

        if (dto is null) return NotFound();
        return dto;
    }

    // ------------------------------------------------------------------
    // GRID paginado (sin filtros) - igual que el tuyo, con AsNoTracking
    // ------------------------------------------------------------------
    [HttpGet("grid")]
    public async Task<PagedResult<PacienteGridItemDto>> Grid([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 20 : pageSize;

        var baseQ = _db.Pacientes.AsNoTracking();
        var total = await baseQ.CountAsync(ct);

        var items = await baseQ
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
            .ToListAsync(ct);

        return new PagedResult<PacienteGridItemDto>
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        };
    }

    // ------------------------------------------------------------------
    // HISTORIAL por paciente (paginado)
    // ------------------------------------------------------------------
    [HttpGet("{pacienteId:guid}/historial")]
    public async Task<PagedResult<VisitaRowDto>> Historial(
        Guid pacienteId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] EstadoHistoria? estado = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] bool soloPendientes = false,
        CancellationToken ct = default)
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

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(v => v.Fecha)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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
            .ToListAsync(ct);

        return new PagedResult<VisitaRowDto>
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        };
    }

    // ------------------------------------------------------------------
    // GET BY ID - devuelve DTO completo (consistente con Create)
    // ------------------------------------------------------------------
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PacienteItem>> GetById(Guid id, CancellationToken ct)
    {
        var dto = await _db.Pacientes
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(PacienteSelectors.ToItem)
            .FirstOrDefaultAsync(ct);

        if (dto is null) return NotFound();
        return dto;
    }
}
