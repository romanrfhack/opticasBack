using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Optica.Application.Visitas.Dtos;
using Optica.Domain.Dtos;
using Optica.Domain.Entities;
using Optica.Domain.Enums;
using Optica.Infrastructure.Identity;
using Optica.Infrastructure.Persistence;

using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Optica.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HistoriasController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    public HistoriasController(AppDbContext db, UserManager<AppUser> um) { _db = db; _userManager = um; }  

    public sealed record MaterialCHDto(Guid materialId, string? observaciones);

    // tipo: "Esferico" | "Torico" | "Otro"
    public sealed record LcDto(string tipo, string? marca, string? modelo, string? observaciones);

    public sealed record ArmazonDto(Guid productoId, string? observaciones);

    public sealed record CrearHistoriaRequest(
        Guid pacienteId,
        string? observaciones,
        AgudezaDto[] av,
        RxDto[] rx,
        MaterialCHDto[] materiales,
        LcDto[] lentesContacto,
        ArmazonDto[] armazones, 
        decimal total
    );

    //DTOS
    public record AgudezaDto(string Condicion, string Ojo, int Denominador);

    public record RxDto(string Ojo, string Distancia, decimal? Esf, decimal? Cyl, int? Eje, decimal? Add, string? Dip, decimal? AltOblea);
    public record MaterialSeleccionadoDto(Guid MaterialId, string Descripcion, string? Marca, string? Observaciones);
    public record LenteContactoDto(string Tipo, string? Marca, string? Modelo, string? Observaciones);

    public record PagoDto(Guid Id, string Metodo, decimal Monto, string? Autorizacion, string? Nota, DateTime Fecha);

    public record EnviarLabRequestDto(decimal Total, List<PagoCrearDto>? Pagos, DateTime? FechaEstimadaEntrega);

    public record PagoCrearDto(decimal Monto, string Metodo, string? Autorizacion, string? Nota);

    public sealed record ConceptoCrearDto(string Concepto, decimal Monto, string? Observaciones);
    public sealed record GuardarConceptosRequest(List<ConceptoCrearDto> Conceptos);

    [HttpPost]
    public async Task<ActionResult<object>> Crear(CrearHistoriaRequest req)
    {
        // Obtener información del usuario
        var sucursalId = Guid.Parse(User.FindFirst("sucursalId")!.Value);

        string? GetClaim(params string[] types)
            => types.Select(t => User.FindFirst(t)?.Value)
                .FirstOrDefault(v => !string.IsNullOrEmpty(v));

        var userIdStr = GetClaim(JwtRegisteredClaimNames.Sub, ClaimTypes.NameIdentifier, "sub");
        var userEmail = GetClaim(JwtRegisteredClaimNames.Email, ClaimTypes.Email, "email");
        var userName = GetClaim("name", ClaimTypes.Name, JwtRegisteredClaimNames.UniqueName) ?? User.Identity?.Name;

        if (string.IsNullOrEmpty(userIdStr))
        {
            return BadRequest("No se pudo identificar al usuario");
        }

        var userId = Guid.Parse(userIdStr);

        // Crear la visita
        var visita = new HistoriaClinicaVisita
        {
            Id = Guid.NewGuid(),
            PacienteId = req.pacienteId,
            SucursalId = sucursalId,
            UsuarioId = userId, // ✅ Guardar quién creó
            UsuarioNombre = userName ?? "Usuario", // ✅ Guardar nombre
            Estado = EstadoHistoria.Creada,
            Observaciones = req.observaciones,
            Fecha = DateTime.UtcNow
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
                Dip = r.Dip, // ✅ Corregir: era DIP, ahora Dip
                AltOblea = r.AltOblea
            });
        }

        // Materiales
        foreach (var m in req.materiales ?? Array.Empty<MaterialCHDto>())
        {
            visita.Materiales.Add(new PrescripcionMaterial
            {
                Id = Guid.NewGuid(),
                VisitaId = visita.Id,
                MaterialId = m.materialId,
                Observaciones = m.observaciones ?? ""
            });
        }

        // ✅ NUEVO: Armazones
        foreach (var armazon in req.armazones ?? Array.Empty<ArmazonDto>())
        {
            visita.Armazon.Add(new PrescripcionArmazon
            {
                Id = Guid.NewGuid(),
                VisitaId = visita.Id,
                ProductoId = armazon.productoId,
                Observaciones = armazon.observaciones ?? ""
            });
        }

        // Lentes de contacto
        foreach (var lc in req.lentesContacto ?? Array.Empty<LcDto>())
        {
            if (!Enum.TryParse<TipoLenteContacto>(lc.tipo, true, out var tipo))
                tipo = TipoLenteContacto.Otro;

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

        visita.Total = req.total;

        _db.Visitas.Add(visita);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = visita.Id }, new { id = visita.Id });
    }

    private static int Clamp(int value, int min, int max)
    {
        return value < min ? min : value > max ? max : value;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<VisitaCompletaDto>> GetById(Guid id)
    {
        var visita = await _db.Visitas
            .Include(v => v.Agudezas)
            .Include(v => v.Rx)
            .Include(v => v.Materiales)
                .ThenInclude(m => m.Material)
            .Include(v => v.Armazon)
                .ThenInclude(a => a.Producto)
            .Include(v => v.LentesContacto)
            .Include(v => v.Paciente)
            .Include(v => v.Sucursal) // ✅ INCLUIR SUCURSAL
            .Include(v => v.Conceptos)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (visita == null)
            return NotFound();

        var visitaDto = new VisitaCompletaDto
        {
            Id = visita.Id,
            PacienteId = visita.PacienteId,
            SucursalId = visita.SucursalId,
            NombreSucursal = visita.Sucursal.Nombre, // ✅ USAR EL NOMBRE DIRECTAMENTE
            UsuarioId = visita.UsuarioId,
            UsuarioNombre = visita.UsuarioNombre,
            Fecha = visita.Fecha,
            Estado = visita.Estado.ToString(),
            Total = visita.Total,
            ACuenta = visita.ACuenta,
            Resta = visita.Resta,
            FechaEnvioLaboratorio = visita.FechaEnvioLaboratorio,
            FechaEstimadaEntrega = visita.FechaEstimadaEntrega,
            FechaRecibidoSucursal = visita.FechaRecibidoSucursal,
            FechaEntregaCliente = visita.FechaEntregaCliente,
            Observaciones = visita.Observaciones,

            // Paciente
            Paciente = new PacienteDto
            {
                Id = visita.Paciente.Id,
                Nombre = visita.Paciente.Nombre,
                Edad = visita.Paciente.Edad,
                Telefono = visita.Paciente.Telefono,
                Ocupacion = visita.Paciente.Ocupacion,
                Direccion = visita.Paciente.Direccion
            },

            // Agudezas
            Agudezas = visita.Agudezas.Select(a => new AgudezaVisualDto
            {
                Id = a.Id,
                Condicion = a.Condicion.ToString(),
                Ojo = a.Ojo.ToString(),
                Denominador = a.Denominador
            }).ToList(),

            // RX
            Rx = visita.Rx.Select(r => new RxMedicionDto
            {
                Id = r.Id,
                Ojo = r.Ojo.ToString(),
                Distancia = r.Distancia.ToString(),
                Esf = r.Esf,
                Cyl = r.Cyl,
                Eje = r.Eje,
                Add = r.Add,
                Dip = r.Dip,
                AltOblea = r.AltOblea
            }).ToList(),

            // Materiales
            Materiales = visita.Materiales.Select(m => new PrescripcionMaterialDto
            {
                Id = m.Id,
                MaterialId = m.MaterialId,
                Observaciones = m.Observaciones,
                Material = new MaterialDto
                {
                    Id = m.Material.Id,
                    Descripcion = m.Material.Descripcion,
                    Marca = m.Material.Marca
                }
            }).ToList(),

            // Armazones
            Armazones = visita.Armazon.Select(a => new PrescripcionArmazonDto
            {
                Id = a.Id,
                ProductoId = a.ProductoId,
                Observaciones = a.Observaciones,
                Producto = new ProductoDto
                {
                    Id = a.Producto.Id,
                    Sku = a.Producto.Sku,
                    Nombre = a.Producto.Nombre,
                    Categoria = a.Producto.Categoria.ToString()
                }
            }).ToList(),

            // Lentes de contacto
            LentesContacto = visita.LentesContacto.Select(lc => new PrescripcionLenteContactoDto
            {
                Id = lc.Id,
                Tipo = lc.Tipo.ToString(),
                Marca = lc.Marca,
                Modelo = lc.Modelo,
                Observaciones = lc.Observaciones
            }).ToList(),

            //CONCEPTOS
            Conceptos = visita.Conceptos.Select(c => new VisitaConceptoDto
            {
                Id = c.Id,
                Concepto = c.Concepto,
                Monto = c.Monto,
                UsuarioNombre = c.UsuarioNombre,
                Fecha = c.TimestampUtc,
                Observaciones = c.Observaciones
            }).ToList()
        };

        return visitaDto;
    }

    [HttpGet("paciente/{pacienteId}")]
    public async Task<ActionResult<List<UltimaHistoriaItem>>> GetByPaciente(Guid pacienteId)
    {
        var visitas = await _db.Visitas
            .Where(v => v.PacienteId == pacienteId)
            .Include(v => v.Sucursal) // ✅ INCLUIR SUCURSAL
            .OrderByDescending(v => v.Fecha)
            .Take(10)
            .Select(v => new UltimaHistoriaItem
            {
                Id = v.Id,
                Fecha = v.Fecha,
                Estado = v.Estado.ToString(),
                Total = v.Total,
                ACuenta = v.ACuenta,
                Resta = v.Resta,
                NombreSucursal = v.Sucursal != null ? v.Sucursal.Nombre : "Sucursal no disponible", 
                UsuarioNombre = v.UsuarioNombre
            })
            .ToListAsync();

        return visitas;
    }

    [HttpGet("ultimas/{pacienteId:guid}")]
    public async Task<IEnumerable<UltimaVisitaDto>> Ultimas(Guid pacienteId, [FromQuery] int take = 5)
    {
        // Para ordenar Ojo y Distancia de forma estable en EF
        // (ajusta si tus enums tienen otro orden numérico)
        // Suponiendo: Ojo.OD = 0, Ojo.OI = 1; RxDistancia.Lejos = 0, RxDistancia.Cerca = 1
        return await _db.Visitas
            .Where(v => v.PacienteId == pacienteId)
            .OrderByDescending(v => v.Fecha)
            .Take(take)
            .Select(v => new UltimaVisitaDto
            {
                Id = v.Id,
                Fecha = v.Fecha,
                Estado = v.Estado.ToString(), // o .ToString() si es enum
                Total = v.Total,
                ACuenta = v.Pagos.Sum(p => (decimal?)p.Monto) ?? 0m,
                Resta = (v.Total ?? 0m) - (v.Pagos.Sum(p => (decimal?)p.Monto) ?? 0m),

                // Solo el último pago (opcional; si no lo necesitas, quítalo)
                UltimoPago = v.Pagos
                    .OrderByDescending(p => p.Fecha)
                    .Select(p => new PagoMiniDto
                    {
                        Fecha = p.Fecha,
                        Monto = p.Monto,
                        Metodo = p.Metodo.ToString(), // si es enum
                        Autorizacion = p.Autorizacion,
                        Nota = p.Nota
                    })
                    .FirstOrDefault(),

                // RX por visita
                Rx = v.Rx
                    .OrderBy(m => m.Distancia)   // Lejos (0) antes que Cerca (1)
                    .ThenBy(m => m.Ojo)          // OD (0) antes que OI (1)
                    .Select(m => new RxMedicionDto
                    {
                        Ojo = m.Ojo.ToString(),
                        Distancia = m.Distancia.ToString(),
                        Esf = m.Esf,
                        Cyl = m.Cyl,
                        Eje = m.Eje,
                        Add = m.Add,
                        Dip = m.Dip,
                        AltOblea = m.AltOblea
                    })
                    .ToList()
            })
            .ToListAsync();
    }

    [HttpGet("visitas/{id:guid}")]
    public async Task<ActionResult<VisitaDetalleDto>> Detalle(Guid id)
    {
        var dto = await _db.Visitas
            .Where(v => v.Id == id)
            .Select(v => new VisitaDetalleDto
            {
                Id = v.Id,
                Fecha = v.Fecha,
                Estado = v.Estado.ToString(),
                Total = v.Total,
                ACuenta = v.Pagos.Sum(p => (decimal?)p.Monto) ?? 0m,
                Resta = (v.Total ?? 0m) - (v.Pagos.Sum(p => (decimal?)p.Monto) ?? 0m),

                PacienteId = v.PacienteId,
                PacienteNombre = v.Paciente.Nombre,
                PacienteTelefono = v.Paciente.Telefono,

                Rx = v.Rx
                    .OrderBy(m => m.Distancia)   // o usa ternarios si tus enums difieren
                    .ThenBy(m => m.Ojo)
                    .Select(m => new RxMedicionDto
                    {
                        Ojo = m.Ojo.ToString(),
                        Distancia = m.Distancia.ToString(),
                        Esf = m.Esf,
                        Cyl = m.Cyl,
                        Eje = m.Eje,
                        Add = m.Add,
                        Dip = m.Dip,
                        AltOblea = m.AltOblea
                    }).ToList(),

                Av = v.Agudezas
                    .OrderBy(a => a.Condicion)
                    .ThenBy(a => a.Ojo)
                    .Select(a => new AgudezaVisual()
                    {
                        Ojo = a.Ojo,
                        Condicion = a.Condicion,
                        Denominador = a.Denominador
                    }).ToList(),

                Pagos = v.Pagos
                    .OrderByDescending(p => p.Fecha)
                    .Select(p => new PagoMiniDto
                    {
                        Fecha = p.Fecha,
                        Monto = p.Monto,
                        Metodo = p.Metodo.ToString(),
                        Autorizacion = p.Autorizacion,
                        Nota = p.Nota
                    }).ToList(),

                FechaEstimadaEntrega = v.FechaEstimadaEntrega,
                FechaRecibidaSucursal = v.FechaRecibidoSucursal,
                FechaEntregadaCliente = v.FechaEntregaCliente,

                Materiales = v.Materiales
                    .Select(x => new MaterialSeleccionDto
                    {
                        MaterialId = x.MaterialId,
                        Descripcion = x.Material.Descripcion,
                        Marca = x.Material.Marca,
                        Observaciones = x.Observaciones
                    }).ToList(),

                LentesContacto = v.LentesContacto
                    .Select(x => new LenteContactoSeleccionDto
                    {
                        Tipo = x.Tipo.ToString(),
                        Marca = x.Marca,
                        Modelo = x.Modelo,
                        Observaciones = x.Observaciones
                    }).ToList()
            })
            .FirstOrDefaultAsync();

        if (dto == null) return NotFound();
        return dto;
    }

    [HttpGet("{id:guid}/pagos")]
    public async Task<IEnumerable<object>> ListarPagos(Guid id)
        => await _db.HistoriaPagos.Where(p => p.VisitaId == id)
            .OrderBy(p => p.Fecha)
            .Select(p => new { p.Id, p.Fecha, p.Metodo, p.Monto, p.Autorizacion, p.Nota })
            .ToListAsync();

    [HttpPost("{id:guid}/pagos")]
    public async Task<IActionResult> AgregarPagos(Guid id, PagoDto[] pagos)
    {
        var visita = await _db.Visitas
            .Include(v => v.Pagos)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (visita is null) return NotFound();

        foreach (var pago in pagos)
        {
            if (!Enum.TryParse<MetodoPago>(pago.Metodo, true, out var metodo))
                return BadRequest(new { message = $"Método inválido en pago: {pago.Metodo}" });

            visita.Pagos.Add(new HistoriaPago
            {
                VisitaId = id,
                Metodo = metodo,
                Monto = pago.Monto,
                Autorizacion = pago.Autorizacion,
                Nota = pago.Nota
            });
        }

        // Recalcular totales
        var sumaTotal = visita.Pagos.Sum(p => p.Monto);
        visita.ACuenta = sumaTotal;
        visita.Resta = (visita.Total ?? 0) - sumaTotal;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("en-laboratorio")]
    public async Task<IEnumerable<object>> EnLaboratorio([FromQuery] int take = 100)
        => await _db.Visitas
            .Where(h => h.Estado == EstadoHistoria.EnviadaALaboratorio)
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
        last.Estado = Optica.Domain.Enums.EstadoHistoria.RecibidaEnSucursal;
        last.FechaEnvioLaboratorio = DateTime.UtcNow;
        last.FechaEstimadaEntrega = body.FechaEstimadaEntrega;

        await _db.SaveChangesAsync();
        return NoContent();
    }


    [HttpGet("visitas-costos")]
    public async Task<ActionResult<PagedResult<VisitaCostoRowDto>>> GetVisitasCostos(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] int estado = -1
    )
    {
        var sucursalIdClaim = User.FindFirst("sucursalId")?.Value;
        if (string.IsNullOrWhiteSpace(sucursalIdClaim))
            return Forbid();

        var sucursalId = Guid.Parse(sucursalIdClaim);

        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        var q = _db.Visitas
            .AsNoTracking()
            .Where(v => v.SucursalId == sucursalId);

        //userRole = "Admin"
        if (userRole == "Admin")
        {
            q = _db.Visitas
                .AsNoTracking();
        }

        //userRole = "Mensajero"
        if (userRole == "Mensajero")
        {
            q = _db.Visitas
                .AsNoTracking()
                .Where(v => v.Estado == EstadoHistoria.ListaParaEnvio);
        }

        // Filtro por estado (si lo habilitas)
        // if (estado != -1)
        //     q = q.Where(v => (int)v.Estado == estado);

        if (!string.IsNullOrWhiteSpace(search))
        {
            q = q.Where(v => (v.Paciente.Nombre).Contains(search));
        }

        var total = await q.CountAsync();

        var rows = await q
            .OrderByDescending(v => v.Fecha)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new VisitaCostoRowDto(
                v.Id,
                v.Fecha,
                v.Paciente.Nombre,
                v.UsuarioNombre,
                (int)v.Estado,
                v.Total,
                v.ACuenta,
                v.Resta,
                v.Sucursal.Nombre,
                _db.VisitaStatusHistory
                    .Where(h => h.VisitaId == v.Id)
                    .OrderByDescending(h => h.TimestampUtc)
                    .Select(h => h.LabTipo)
                    .FirstOrDefault(),
                _db.VisitaStatusHistory
                    .Where(h => h.VisitaId == v.Id)
                    .OrderByDescending(h => h.TimestampUtc)
                    .Select(h => h.LabNombre)
                    .FirstOrDefault(),
                // NUEVO: último TimestampUtc en VisitaStatusHistory para esta visita
                _db.VisitaStatusHistory
                    .Where(h => h.VisitaId == v.Id)
                    .OrderByDescending(h => h.TimestampUtc)
                    .Select(h => (DateTimeOffset?)h.TimestampUtc)
                    .FirstOrDefault()
            ))
            .ToListAsync();

        return Ok(new PagedResult<VisitaCostoRowDto>(rows, page, pageSize, total));
    }



    [HttpPost("{id:guid}/status")]
    public async Task<ActionResult<ChangeVisitaStatusResponse>> ChangeStatus(Guid id, [FromBody] ChangeVisitaStatusRequest body)
    {
        var sucursalId = Guid.Parse(User.FindFirst("sucursalId")!.Value);

        string? GetClaim(params string[] types)
            => types.Select(t => User.FindFirst(t)?.Value)
                .FirstOrDefault(v => !string.IsNullOrEmpty(v));

        var usuarioId = GetClaim(JwtRegisteredClaimNames.Sub, ClaimTypes.NameIdentifier, "sub");
        var usuarioNom = GetClaim("name", ClaimTypes.Name, JwtRegisteredClaimNames.UniqueName) ?? User.Identity?.Name;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        var visita = await _db.Visitas.FirstOrDefaultAsync(v => v.Id == id && v.SucursalId == sucursalId);

        //userRole = "Admin"
        if (userRole == "Admin")
        {
            visita = await _db.Visitas.FirstOrDefaultAsync(v => v.Id == id );
        }

        //userRole = "Mensajero"
        if (userRole == "Mensajero")
        {
            visita = await _db.Visitas.FirstOrDefaultAsync(v => v.Id == id && v.Estado == EstadoHistoria.ListaParaEnvio);
        }
        if (visita is null) return NotFound("Visita no encontrada en tu sucursal.");

        var fromStatus = visita.Estado;
        var toStatusValue = body.ToStatus;

        // 1) Validar que el número corresponde a un estado válido
        if (!Enum.IsDefined(typeof(EstadoHistoria), toStatusValue))
            return BadRequest($"Estado inválido: {toStatusValue}. Estados válidos: 0-{Enum.GetValues<EstadoHistoria>().Length - 1}");

        var nuevoEstado = (EstadoHistoria)toStatusValue;

        // 2) Validar que es el siguiente estado en secuencia
        if ((int)nuevoEstado != (int)fromStatus + 1)
            return BadRequest($"Solo se permite avanzar al estado siguiente. Estado actual: {(int)fromStatus}, próximo esperado: {(int)fromStatus + 1}");

        // 3) Validaciones específicas por estado
        if (nuevoEstado == EstadoHistoria.EnviadaALaboratorio)
        {
            if (string.IsNullOrWhiteSpace(body.LabTipo))
                return BadRequest("LabTipo es requerido para 'Enviada a laboratorio'.");
            if (body.LabTipo is not ("Interno" or "Externo"))
                return BadRequest("LabTipo debe ser 'Interno' o 'Externo'.");
        }

        // 4) Persistencia atómica
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Actualizar estado
            visita.Estado = nuevoEstado;

            // Actualizar fechas según el estado
            UpdateFechasPorEstado(visita, nuevoEstado);

            // Insertar en historial
            Debug.Assert(usuarioId != null, nameof(usuarioId) + " != null");
            Debug.Assert(usuarioNom != null, nameof(usuarioNom) + " != null");
            var entry = new VisitaStatusHistory
            {
                VisitaId = visita.Id,
                FromStatus = fromStatus.ToString(),
                ToStatus = nuevoEstado.ToString(),
                UsuarioId = Guid.Parse(usuarioId),
                UsuarioNombre = usuarioNom,
                SucursalId = sucursalId,
                TimestampUtc = DateTimeOffset.UtcNow,
                Observaciones = body.Observaciones,
                LabTipo = nuevoEstado == EstadoHistoria.EnviadaALaboratorio ? body.LabTipo : null,
                LabId = nuevoEstado == EstadoHistoria.EnviadaALaboratorio ? body.LabId : null,
                LabNombre = nuevoEstado == EstadoHistoria.EnviadaALaboratorio ? body.LabNombre : null
            };

            _db.VisitaStatusHistory.Add(entry);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new ChangeVisitaStatusResponse(
                visita.Id,
                fromStatus.ToString(),
                nuevoEstado.ToString(),
                entry.TimestampUtc
            ));
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            // Log: _logger.LogError(ex, "Error al cambiar estado de visita {VisitaId}", id);
            return StatusCode(500, "Error interno al actualizar el estado");
        }
    }

    private void UpdateFechasPorEstado(HistoriaClinicaVisita visita, EstadoHistoria nuevoEstado)
    {
        switch (nuevoEstado)
        {
            case EstadoHistoria.EnviadaALaboratorio:
                visita.FechaEnvioLaboratorio ??= DateTime.UtcNow;
                break;
            case EstadoHistoria.ListaEnLaboratorio:
                visita.FechaEstimadaEntrega ??= DateTime.UtcNow.AddDays(3);
                break;
            case EstadoHistoria.RecibidaEnSucursal:
                visita.FechaRecibidoSucursal ??= DateTime.UtcNow;
                break;
            case EstadoHistoria.EntregadaAlCliente:
                visita.FechaEntregaCliente ??= DateTime.UtcNow;
                break;
        }
    }

    [HttpGet("{id:guid}/status-history")]
    public async Task<ActionResult<IEnumerable<VisitaStatusHistory>>> GetHistory(Guid id)
    {
        var sucursalId = Guid.Parse(User.FindFirst("sucursalId")!.Value);

        var pertenece = await _db.Visitas.AnyAsync(v => v.Id == id && v.SucursalId == sucursalId);
            if (!pertenece) return NotFound();

            var hist = await _db.VisitaStatusHistory
                .Where(h => h.VisitaId == id)
                .OrderByDescending(h => h.TimestampUtc)
                .ToListAsync();

            return Ok(hist);
        }

    private static readonly Dictionary<string, string[]> Allowed = new()
        {
            ["Creada"] = new[] { "Registrada", "Cancelada" },
            ["Registrada"] = new[] { "Enviada a laboratorio", "Cancelada" },
            ["Enviada a laboratorio"] = new[] { "Lista en laboratorio" },
            ["Lista en laboratorio"] = new[] { "Recibida en sucursal central", "Recibida en sucursal origen" },
            ["Recibida en sucursal central"] = new[] { "Lista para entrega" },
            ["Recibida en sucursal origen"] = new[] { "Lista para entrega" },
            ["Lista para entrega"] = new[] { "Entregada al cliente" },
            ["Entregada al cliente"] = Array.Empty<string>(),
            ["Cancelada"] = Array.Empty<string>()
        };

    [HttpPost("{id:guid}/conceptos")]
    public async Task<IActionResult> GuardarConceptos(Guid id, [FromBody] GuardarConceptosRequest body)
    {
        if (body?.Conceptos is null || body.Conceptos.Count == 0)
            return BadRequest("Debe enviar al menos un concepto.");

        // Claims (sucursal y usuario)
        var sucursalIdClaim = User.FindFirst("sucursalId")?.Value;
        if (string.IsNullOrWhiteSpace(sucursalIdClaim))
            return Forbid();

        var sucursalId = Guid.Parse(sucursalIdClaim);

        string? GetClaim(params string[] types)
            => types.Select(t => User.FindFirst(t)?.Value).FirstOrDefault(v => !string.IsNullOrEmpty(v));

        var userIdStr = GetClaim(JwtRegisteredClaimNames.Sub, ClaimTypes.NameIdentifier, "sub");
        var userName = GetClaim("name", ClaimTypes.Name, JwtRegisteredClaimNames.UniqueName) ?? User.Identity?.Name;

        if (string.IsNullOrWhiteSpace(userIdStr) || string.IsNullOrWhiteSpace(userName))
            return BadRequest("No se pudo identificar al usuario.");

        var userId = Guid.Parse(userIdStr);

        // Traer la visita validando que pertenezca a la sucursal
        var visita = await _db.Visitas
            .Include(v => v.Conceptos)
            .FirstOrDefaultAsync(v => v.Id == id && v.SucursalId == sucursalId);

        if (visita is null)
            return NotFound("Visita no encontrada en tu sucursal.");

        // Validaciones simples
        foreach (var c in body.Conceptos)
        {
            if (string.IsNullOrWhiteSpace(c.Concepto))
                return BadRequest("Todos los conceptos deben tener nombre.");
            if (c.Monto < 0)
                return BadRequest($"El concepto '{c.Concepto}' no puede tener monto negativo.");
        }

        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 1) Reemplazar conceptos existentes
            if (visita.Conceptos.Any())
            {
                _db.VisitaConceptos.RemoveRange(visita.Conceptos);
                await _db.SaveChangesAsync();
            }

            // 2) Insertar nuevos conceptos
            var now = DateTimeOffset.UtcNow;
            var nuevos = body.Conceptos.Select(c => new VisitaConcepto
            {
                Id = Guid.NewGuid(),
                VisitaId = visita.Id,
                Concepto = c.Concepto.Trim(),
                Monto = c.Monto,
                UsuarioId = userId,
                UsuarioNombre = userName!,
                SucursalId = sucursalId,
                TimestampUtc = now,
                Observaciones = string.IsNullOrWhiteSpace(c.Observaciones) ? null : c.Observaciones!.Trim()
            }).ToList();

            await _db.VisitaConceptos.AddRangeAsync(nuevos);

            // 3) Recalcular totales de la Visita
            var total = nuevos.Sum(x => x.Monto);
            visita.Total = total;
            visita.ACuenta = 0m;         // Antes de registrar pagos
            visita.Resta = total;        // Resta = Total (sin pagos)

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new
            {
                visitaId = visita.Id,
                total,
                conceptos = nuevos.Select(n => new { n.Id, n.Concepto, n.Monto, n.TimestampUtc })
            });
        }
        catch (Exception)
        {
            await tx.RollbackAsync();
            return StatusCode(500, "Error al guardar conceptos.");
        }
    }

    private static bool IsAllowedTransition(string from, string? to)
            => to is not null && Allowed.TryGetValue(from, out var next) && next.Contains(to);
        
    public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

}