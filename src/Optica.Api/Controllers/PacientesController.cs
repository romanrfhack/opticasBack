using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica.Infrastructure.Persistence;
using Optica.Domain.Entities;

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

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Paciente>> GetById(Guid id)
        => await _db.Pacientes.FindAsync(id) is { } p ? Ok(p) : NotFound();
}
