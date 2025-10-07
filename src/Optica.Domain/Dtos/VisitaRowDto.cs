using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Optica.Domain.Dtos
{
    public sealed class VisitaRowDto
    {
        public Guid Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Estado { get; set; } = default!;
        public decimal? Total { get; set; }
        public decimal ACuenta { get; set; }
        public decimal Resta { get; set; }

        public DateTime? UltimoPagoFecha { get; set; }
        public decimal? UltimoPagoMonto { get; set; }

        public DateTime? FechaEstimadaEntrega { get; set; }
        public DateTime? FechaRecibidaSucursal { get; set; }
        public DateTime? FechaEntregadaCliente { get; set; }
    }
}
