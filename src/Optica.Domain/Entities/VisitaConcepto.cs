using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Optica.Domain.Entities
{
    public class VisitaConcepto
    {
        public Guid Id { get; set; }

        [Required]
        public Guid VisitaId { get; set; }

        [Required, MaxLength(128)]
        public string Concepto { get; set; } = default!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        [Required]
        public Guid UsuarioId { get; set; }

        [Required, MaxLength(128)]
        public string UsuarioNombre { get; set; } = default!;

        [Required]
        public Guid SucursalId { get; set; }

        public DateTimeOffset TimestampUtc { get; set; }

        [MaxLength(1024)]
        public string? Observaciones { get; set; }

        // Relación opcional (no obligatoria)
        public HistoriaClinicaVisita? Visita { get; set; }
    }
}