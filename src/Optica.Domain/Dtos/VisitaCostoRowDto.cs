using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Optica.Domain.Dtos
{
    public sealed record VisitaCostoRowDto(
        Guid Id,
        DateTime Fecha,
        string Paciente,         // Nombre completo
        string UsuarioNombre,    // Quien atendió
        int Estado,           // Texto del enum
        decimal? Total,
        decimal? ACuenta,
        decimal? Resta,
        DateTimeOffset? FechaUltimaActualizacion // <-- NUEVO
    );

    public sealed record ChangeVisitaStatusRequest(
        int ToStatus,
        string? Observaciones,
        string? LabTipo,             // "Interno" | "Externo" (solo si ToStatus = Enviada a laboratorio)
        Guid? LabId,
        string? LabNombre
    );

    public sealed record ChangeVisitaStatusResponse(
        Guid VisitaId,
        string FromStatus,
        string ToStatus,
        DateTimeOffset TimestampUtc
    );

}
