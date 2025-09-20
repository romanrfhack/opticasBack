using Optica.Domain.Enums;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Optica.Domain.Entities
{
    public class PrescripcionMaterial
    {
        public Guid Id { get; set; }

        public Guid VisitaId { get; set; }
        public HistoriaClinicaVisita Visita { get; set; } = default!;

        public Guid MaterialId { get; set; }
        public Material Material { get; set; } = default!;

        public string? Observaciones { get; set; }
    }
}
