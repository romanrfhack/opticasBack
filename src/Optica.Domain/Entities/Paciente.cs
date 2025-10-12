using System.ComponentModel.DataAnnotations;

namespace Optica.Domain.Entities
{
    public class Paciente
    {
        public Guid Id { get; set; }

        [MaxLength(200)]
        public string Nombre { get; set; } = default!;

        public int Edad { get; set; }

        [MaxLength(30)]
        public string Telefono { get; set; } = "";

        [MaxLength(120)]
        public string Ocupacion { get; set; } = "";

        [MaxLength(300)]
        public string? Direccion { get; set; }

        public Guid SucursalIdAlta { get; set; }

        // Nota: en BD se establecerá con GETUTCDATE() (OnModelCreating). Este valor local es por si se instancia en memoria.
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        // Auditoría
        public Guid? CreadoPorUsuarioId { get; set; }

        [MaxLength(200)]
        public string? CreadoPorNombre { get; set; }

        [MaxLength(200)]
        public string? CreadoPorEmail { get; set; }

        // Normalizados (columnas computadas/persistidas; setter privado para EF)
        [MaxLength(200)]
        public string? NombreNormalized { get; private set; }

        [MaxLength(30)]
        public string? TelefonoNormalized { get; private set; }

        // Navegaciones (opcional: útil para Includes)
        public Sucursal? SucursalAlta { get; set; }  // si quieres: config.HasOne<Sucursal>().WithMany().HasForeignKey(x => x.SucursalIdAlta);

        public ICollection<HistoriaClinicaVisita> Visitas { get; set; } = new List<HistoriaClinicaVisita>();
    }
}