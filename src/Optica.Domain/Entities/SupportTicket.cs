using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Optica.Domain.Entities
{
    public class SupportTicket
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public string Email { get; set; } = default!;
        public string Asunto { get; set; } = default!;
        public string Mensaje { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Estado { get; set; } = "Abierto"; // Abierto, EnProceso, Cerrado
    }
}
