using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Optica.Domain.Enums;


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
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        public ICollection<HistoriaClinicaVisita> Visitas { get; set; } = new List<HistoriaClinicaVisita>();
    }

}
