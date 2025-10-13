using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Optica.Domain.Entities
{
    public class PrescripcionArmazon
    {
        public Guid Id { get; set; }

        public Guid VisitaId { get; set; }
        public HistoriaClinicaVisita Visita { get; set; } = default!;

        public Guid ProductoId { get; set; } 
        public Producto Producto { get; set; } = default!; 

        public string? Observaciones { get; set; }
    }
}
