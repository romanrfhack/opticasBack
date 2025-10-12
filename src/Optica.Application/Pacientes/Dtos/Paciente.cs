using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Optica.Application.Pacientes.Dtos
{
    public record PacienteItem(
        Guid Id,
        string Nombre,
        int Edad,
        string Telefono,
        string Ocupacion,
        Guid SucursalId,
        string? SucursalNombre,
        DateTime FechaRegistroUtc,
        CreadorDto CreadoPor
    );

    public record CreadorDto(
        Guid? UsuarioId,
        string? Nombre,
        string? Email
    );
}
