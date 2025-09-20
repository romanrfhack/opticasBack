using Optica.Domain.Enums;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Optica.Domain.Entities
{
    public class RxMedicion
    {
        public Guid Id { get; set; }

        public Guid VisitaId { get; set; }
        public HistoriaClinicaVisita Visita { get; set; } = default!;

        public Ojo Ojo { get; set; }                 // OD / OI
        public RxDistancia Distancia { get; set; }   // Lejos / Cerca

        public decimal? Esf { get; set; }
        public decimal? Cyl { get; set; }
        public int? Eje { get; set; }
        public decimal? Add { get; set; }

        // Permite valores como "55-70"
        public string? Dip { get; set; }

        public decimal? AltOblea { get; set; }
    }
}
