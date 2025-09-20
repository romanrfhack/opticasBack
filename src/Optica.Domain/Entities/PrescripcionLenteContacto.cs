using Optica.Domain.Enums;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Optica.Domain.Entities
{
    public class PrescripcionLenteContacto
    {
        public Guid Id { get; set; }

        public Guid VisitaId { get; set; }
        public HistoriaClinicaVisita Visita { get; set; } = default!;

        public TipoLenteContacto Tipo { get; set; } // Esferico / Torico / Otro
        public string? Marca { get; set; }          // ACUVUE OASYS, etc.
        public string? Modelo { get; set; }         // Biofinity, ULTRA, ...
        public string? Observaciones { get; set; }
    }
}
