using System.Linq.Expressions;
using Optica.Application.Pacientes.Dtos;
using Optica.Domain.Entities;

namespace Optica.Application.Pacientes.Selectors
{
    public static class PacienteSelectors
    {
        public static readonly Expression<Func<Paciente, PacienteItem>> ToItem =
            p => new PacienteItem(
                p.Id,
                p.Nombre,
                p.Edad,
                p.Telefono,
                p.Ocupacion,
                p.SucursalIdAlta,
                p.SucursalAlta != null ? p.SucursalAlta.Nombre : null,
                p.FechaRegistro, // UTC (default en SQL)
                new CreadorDto(p.CreadoPorUsuarioId, p.CreadoPorNombre, p.CreadoPorEmail)
            );
    }
}