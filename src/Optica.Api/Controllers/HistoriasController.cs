using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Optica.Domain.Entities;
using Optica.Domain.Enums;
using Optica.Infrastructure.Persistence;

using System.Security.Claims;

using static System.Math;

namespace Optica.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HistoriasController : ControllerBase
{
    private readonly AppDbContext _db;
    public HistoriasController(AppDbContext db) { _db = db; }

    private Guid? GetUserSucursalId()
    {
        var val = User?.FindFirstValue("sucursalId");
        return Guid.TryParse(val, out var g) ? g : null;
    }

    // DTOs
    //public sealed record AgudezaDto(string condicion, string ojo, int denominador);

    // ojo: "OD"/"OI", distancia: "Lejos"/"Cerca"
    //public sealed record RxDto(string ojo, string distancia,
    //    decimal? esf, decimal? cyl, int? eje, decimal? add,
    //    string? dip, decimal? altOblea);

    public sealed record MaterialDto(Guid materialId, string? observaciones);

    // tipo: "Esferico" | "Torico" | "Otro"
    public sealed record LcDto(string tipo, string? marca, string? modelo, string? observaciones);

    // metodo: "Efectivo" | "Tarjeta"
    //public sealed record PagoDto(decimal monto, string metodo, string? autorizacion, string? nota);

    public sealed record CrearHistoriaRequest(
        Guid pacienteId,
        string? observaciones,
        AgudezaDto[] av,
        RxDto[] rx,
        MaterialDto[] materiales,
        LcDto[] lentesContacto
    );

    //DTOS
    

    public record UltimaHistoriaItemDto(Guid Id, DateTime Fecha, string Estado, decimal? Total, decimal? ACuenta, decimal? Resta);

    public record HistoriaResumenDto(
        Guid Id,
        DateTime Fecha,
        string Paciente,
        string Telefono,
        string Estado,
        decimal? Total,
        decimal? Resta,
        DateTime? FechaEnvioLaboratorio,
        DateTime? FechaEstimadaEntrega
    );

    public record AgudezaDto(string Condicion, string Ojo, int Denominador);

    public record HistoriaDetalleDto(
        Guid Id,
        DateTime Fecha,
        string Paciente,
        string Telefono,
        string? Observaciones,
        string Estado,
        IEnumerable<AgudezaDto> AV,
        IEnumerable<RxDto> RX,
        IEnumerable<MaterialSeleccionadoDto> Materiales,
        IEnumerable<LenteContactoDto> LentesContacto,
        IEnumerable<PagoDto> Pagos,
        decimal? Total,
        decimal? ACuenta,
        decimal? Resta,
        DateTime? FechaEnvioLaboratorio,
        DateTime? FechaEstimadaEntrega
    );

    public record RxDto(string Ojo, string Distancia, decimal? Esf, decimal? Cyl, int? Eje, decimal? Add, string? DIP, decimal? AltOblea);
    public record MaterialSeleccionadoDto(Guid MaterialId, string Descripcion, string? Marca, string? Observaciones);
    public record LenteContactoDto(string Tipo, string? Marca, string? Modelo, string? Observaciones);

    public record PagoDto(Guid Id, string Metodo, decimal Monto, string? Autorizacion, string? Nota, DateTime Fecha);

    public record EnviarLabRequestDto(
        decimal Total,
        List<PagoCrearDto>? Pagos,
        DateTime? FechaEstimadaEntrega
    );

    public record PagosMultiplesDto(List<PagoCrearDto> Pagos);

    public record PagoCrearDto(decimal Monto, string Metodo, string? Autorizacion, string? Nota);

    [HttpPost]
    public async Task<ActionResult<object>> Crear(CrearHistoriaRequest req)
    {
        var sucursalId = Guid.Parse(User.FindFirst("sucursalId")!.Value);

        var visita = new HistoriaClinicaVisita
        {
            Id = Guid.NewGuid(),
            PacienteId = req.pacienteId,
            SucursalId = sucursalId,
            Estado = EstadoHistoria.Borrador,
            Observaciones = req.observaciones
        };

        // Agudezas
        foreach (var a in req.av ?? Array.Empty<AgudezaDto>())
        {
            if (!Enum.TryParse<CondicionAV>(a.Condicion, true, out var cond)) continue;
            if (!Enum.TryParse<Ojo>(a.Ojo, true, out var ojo)) continue;

            visita.Agudezas.Add(new AgudezaVisual
            {
                Id = Guid.NewGuid(),
                VisitaId = visita.Id,
                Condicion = cond,
                Ojo = ojo,
                Denominador = Clamp(a.Denominador, 10, 200)
            });
        }

        // RX (4 filas: Lejos/Cerca × OD/OI)
        foreach (var r in req.rx ?? Array.Empty<RxDto>())
        {
            if (!Enum.TryParse<Ojo>(r.Ojo, true, out var ojo)) continue;
            if (!Enum.TryParse<RxDistancia>(r.Distancia, true, out var dist)) continue;

            visita.Rx.Add(new RxMedicion
            {
                Id = Guid.NewGuid(),
                VisitaId = visita.Id,
                Ojo = ojo,
                Distancia = dist,
                Esf = r.Esf,
                Cyl = r.Cyl,
                Eje = r.Eje,
                Add = r.Add,
                Dip = r.DIP,
                AltOblea = r.AltOblea
            });
        }

        // Materiales
        foreach (var m in req.materiales ?? Array.Empty<MaterialDto>())
        {
            visita.Materiales.Add(new PrescripcionMaterial
            {
                Id = Guid.NewGuid(),
                VisitaId = visita.Id,
                MaterialId = m.materialId,
                Observaciones = m.observaciones ?? ""
            });
        }

        // Lentes de contacto
        foreach (var lc in req.lentesContacto ?? Array.Empty<LcDto>())
        {
            if (!Enum.TryParse<TipoLenteContacto>(lc.tipo, true, out var tipo)) tipo = TipoLenteContacto.Otro;

            visita.LentesContacto.Add(new PrescripcionLenteContacto
            {
                Id = Guid.NewGuid(),
                VisitaId = visita.Id,
                Tipo = tipo,
                Marca = lc.marca,
                Modelo = lc.modelo,
                Observaciones = lc.observaciones
            });
        }

        _db.Visitas.Add(visita);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = visita.Id }, new { id = visita.Id });
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<HistoriaDetalleDto>> GetById(Guid id)
    {
        var h = await _db.Visitas
            .AsNoTracking()
            .Include(x => x.Paciente)
            .Include(x => x.Visitas.OrderByDescending(v => v.Fecha))
                .ThenInclude(v => v.Pagos)
            .Include(x => x.Visitas)
                .ThenInclude(v => v.Agudezas)
            .Include(x => x.Visitas)
                .ThenInclude(v => v.Rx)
            .Include(x => x.Visitas)
                .ThenInclude(v => v.Materiales)
                    .ThenInclude(m => m.Material)
            .Include(x => x.Visitas)
                .ThenInclude(v => v.LentesContacto)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (h == null) return NotFound();

        var last = h.Visitas.OrderByDescending(v => v.Fecha).FirstOrDefault();
        if (last == null) return NotFound("Historia sin visitas.");

        var dto = new HistoriaDetalleDto(
            Id: h.Id,
            Fecha: last.Fecha,
            Paciente: h.Paciente.Nombre,
            Telefono: h.Paciente.Telefono,
            Observaciones: last.Observaciones,
            Estado: last.Estado.ToString(),
            AV: last.Agudezas.Select(a => new AgudezaDto(a.Condicion.ToString(), a.Ojo.ToString(), a.Denominador)),
            RX: last.Rx.Select(r => new RxDto(r.Ojo.ToString(), r.Distancia.ToString(), r.Esf, r.Cyl, r.Eje, r.Add, r.Dip, r.AltOblea)),
            Materiales: last.Materiales.Select(m => new MaterialSeleccionadoDto(m.MaterialId, m.Material.Descripcion, m.Material.Marca, m.Observaciones)),
            LentesContacto: last.LentesContacto.Select(l => new LenteContactoDto(l.Tipo.ToString(), l.Marca, l.Modelo, l.Observaciones)),
            Pagos: last.Pagos.OrderBy(p => p.Fecha).Select(p => new PagoDto(p.Id, p.Metodo.ToString(), p.Monto, p.Autorizacion, p.Nota, p.Fecha)),
            Total: last.Total,
            ACuenta: last.ACuenta,
            Resta: last.Resta,
            FechaEnvioLaboratorio: last.FechaEnvioLaboratorio,
            FechaEstimadaEntrega: last.FechaEstimadaEntrega
        );

        return Ok(dto);
    }

    [HttpGet("ultimas/{pacienteId:guid}")]
    public async Task<IEnumerable<object>> Ultimas(Guid pacienteId, [FromQuery] int take = 5)
        => await _db.Visitas
            .Where(h => h.PacienteId == pacienteId)
            .OrderByDescending(h => h.Fecha)
            .Take(take)
            .Select(h => new { h.Id, h.Fecha, h.Estado, h.Total, ACuenta = h.Pagos.Sum(p => (decimal?)p.Monto) ?? 0, Resta = (h.Total ?? 0) - (h.Pagos.Sum(p => (decimal?)p.Monto) ?? 0) })
            .ToListAsync();

    public sealed record EnviarLabRequest(decimal total, PagoDto[]? pagos, DateTime? fechaEstimadaEntrega);

    [HttpPost("{id:guid}/enviar-lab")]
    public async Task<IActionResult> EnviarALaboratorio(Guid id, EnviarLabRequest req)
    {
        var v = await _db.Visitas.Include(h => h.Pagos).FirstOrDefaultAsync(h => h.Id == id);
        if (v is null) return NotFound();

        v.Estado = EstadoHistoria.EnLaboratorio;
        v.Total = req.total;
        v.FechaEnvioLaboratorio = DateTime.UtcNow;
        v.FechaEstimadaEntrega = req.fechaEstimadaEntrega;

        if (req.pagos is { Length: > 0 })
        {
            foreach (var p in req.pagos)
            {
                if (!Enum.TryParse<MetodoPago>(p.Metodo, true, out var metodo)) continue;

                v.Pagos.Add(new HistoriaPago
                {
                    Id = Guid.NewGuid(),
                    VisitaId = v.Id,
                    Metodo = metodo,
                    Monto = p.Monto,
                    Autorizacion = p.Autorizacion,
                    Nota = p.Nota
                });
            }
        }

        var sum = v.Pagos.Sum(x => x.Monto);
        v.ACuenta = sum;
        v.Resta = (v.Total ?? 0) - sum;

        await _db.SaveChangesAsync();
        // TODO: movimientos de inventario aquí
        return NoContent();
    }

    [HttpGet("{id:guid}/pagos")]
    public async Task<IEnumerable<object>> ListarPagos(Guid id)
        => await _db.HistoriaPagos.Where(p => p.VisitaId == id)
            .OrderBy(p => p.Fecha)
            .Select(p => new { p.Id, p.Fecha, p.Metodo, p.Monto, p.Autorizacion, p.Nota })
            .ToListAsync();

    [HttpPost("{id:guid}/pagos")]
    public async Task<IActionResult> AgregarPago(Guid id, PagoDto pago)
    {
        var v = await _db.Visitas.Include(h => h.Pagos).FirstOrDefaultAsync(h => h.Id == id);
        if (v is null) return NotFound();

        if (!Enum.TryParse<MetodoPago>(pago.Metodo, true, out var metodo))
            return BadRequest(new { message = "Método inválido" });

        v.Pagos.Add(new HistoriaPago
        {
            Id = Guid.NewGuid(),
            VisitaId = v.Id,
            Metodo = metodo,
            Monto = pago.Monto,
            Autorizacion = pago.Autorizacion,
            Nota = pago.Nota
        });

        var sum = v.Pagos.Sum(x => x.Monto);
        v.ACuenta = sum;
        v.Resta = (v.Total ?? 0) - sum;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("en-laboratorio")]
    public async Task<IEnumerable<object>> EnLaboratorio([FromQuery] int take = 100)
        => await _db.Visitas
            .Where(h => h.Estado == EstadoHistoria.EnLaboratorio)
            .Select(h => new {
                h.Id,
                h.FechaEnvioLaboratorio,
                Paciente = h.Paciente.Nombre,
                Telefono = h.Paciente.Telefono,
                Total = h.Total ?? 0,
                ACuenta = h.Pagos.Sum(p => (decimal?)p.Monto) ?? 0,
                Resta = (h.Total ?? 0) - (h.Pagos.Sum(p => (decimal?)p.Monto) ?? 0),
                h.Observaciones,
                h.FechaEstimadaEntrega
            })
            .OrderByDescending(x => x.FechaEnvioLaboratorio)
            .Take(take)
            .ToListAsync();

    

    

    [HttpPost("{id:guid}/enviar-lab")]
    [Authorize]
    public async Task<ActionResult> EnviarALaboratorio(Guid id, [FromBody] EnviarLabRequestDto body)
    {
        var h = await _db.Visitas
            .Include(x => x.Visitas)
                .ThenInclude(v => v.Pagos)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (h == null) return NotFound();

        var last = h.Visitas.OrderByDescending(v => v.Fecha).FirstOrDefault();
        if (last == null) return BadRequest("Historia sin visitas.");

        last.Total = body.Total;
        var pagos = body.Pagos ?? new();
        var acumulado = last.Pagos.Sum(p => p.Monto);

        foreach (var p in pagos)
        {
            last.Pagos.Add(new Optica.Domain.Entities.HistoriaPago()
            {
                Id = Guid.NewGuid(),
                Fecha = DateTime.UtcNow,
                Metodo = Enum.Parse<Optica.Domain.Enums.MetodoPago>(p.Metodo, ignoreCase: true),
                Monto = p.Monto,
                Autorizacion = p.Autorizacion,
                Nota = p.Nota
            });
            acumulado += p.Monto;
        }

        last.ACuenta = acumulado;
        last.Resta = (last.Total ?? 0) - (last.ACuenta ?? 0);
        last.Estado = Optica.Domain.Enums.EstadoHistoria.EnLaboratorio;
        last.FechaEnvioLaboratorio = DateTime.UtcNow;
        last.FechaEstimadaEntrega = body.FechaEstimadaEntrega;

        await _db.SaveChangesAsync();
        return NoContent();
    }
}
