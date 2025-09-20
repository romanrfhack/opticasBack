using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Optica.Domain.Entities
{
    public class Material
    {
        public Guid Id { get; set; }
        public string Descripcion { get; set; } = default!;
        public string? Marca { get; set; }
        public bool Activo { get; set; } = true;
    }
}
