using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Optica.Domain.Dtos
{
    public sealed class PacienteGridItemDto
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = default!;
        public int Edad { get; set; }
        public string? Telefono { get; set; }
        public string? Ocupacion { get; set; }

        public DateTime? UltimaVisitaFecha { get; set; }
        public string? UltimaVisitaEstado { get; set; }
        public decimal? UltimaVisitaTotal { get; set; }
        public decimal UltimaVisitaACuenta { get; set; }
        public decimal UltimaVisitaResta { get; set; }

        public DateTime? UltimoPagoFecha { get; set; }
        public decimal? UltimoPagoMonto { get; set; }

        public bool TieneOrdenPendiente { get; set; }
    }
}
