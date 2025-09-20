using Optica.Domain.Enums;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Optica.Domain.Entities
{
    public class HistoriaClinicaVisita
    {
        public Guid Id { get; set; }

        public Guid PacienteId { get; set; }
        public Paciente Paciente { get; set; } = default!;

        public Guid SucursalId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
        public EstadoHistoria Estado { get; set; } = EstadoHistoria.Borrador;

        // Totales
        public decimal? Total { get; set; }
        public decimal? ACuenta { get; set; }
        public decimal? Resta { get; set; }

        // Fechas de tracking
        public DateTime? FechaEnvioLaboratorio { get; set; }
        public DateTime? FechaEstimadaEntrega { get; set; }
        public DateTime? FechaRecibidoSucursal { get; set; }
        public DateTime? FechaEntregaCliente { get; set; }

        public string? Observaciones { get; set; }

        public ICollection<AgudezaVisual> Agudezas { get; set; } = new List<AgudezaVisual>();
        public ICollection<RxMedicion> Rx { get; set; } = new List<RxMedicion>();
        public ICollection<PrescripcionMaterial> Materiales { get; set; } = new List<PrescripcionMaterial>();
        public ICollection<PrescripcionLenteContacto> LentesContacto { get; set; } = new List<PrescripcionLenteContacto>();
        public ICollection<HistoriaPago> Pagos { get; set; } = new List<HistoriaPago>();
        public ICollection<HistoriaClinicaVisita> Visitas { get; set; } = new List<HistoriaClinicaVisita>();

        // Vínculos opcionales a productos para inventario
        public Guid? ArmazonProductoId { get; set; }
        public Guid? MaterialId { get; set; }
    }
}
