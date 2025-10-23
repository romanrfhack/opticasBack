using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Optica.Domain.Entities
{
    public class VisitaStatusHistory
    {
        public Guid Id { get; set; }
        public Guid VisitaId { get; set; }
        public string FromStatus { get; set; } = default!;
        public string ToStatus { get; set; } = default!;
        public Guid UsuarioId { get; set; }
        public string UsuarioNombre { get; set; } = default!;
        public Guid SucursalId { get; set; }
        public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;
        public string? Observaciones { get; set; }

        public string? LabTipo { get; set; }      // "Interno" | "Externo"
        public Guid? LabId { get; set; }
        public string? LabNombre { get; set; }

        public HistoriaClinicaVisita Visita { get; set; } = default!;
    }

}
