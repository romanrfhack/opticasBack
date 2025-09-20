using Optica.Domain.Enums;

namespace Optica.Domain.Entities
{
    public class AgudezaVisual
    {
        public Guid Id { get; set; }

        public Guid VisitaId { get; set; }
        public HistoriaClinicaVisita Visita { get; set; } = default!;

        public CondicionAV Condicion { get; set; } // SinLentes / ConLentes
        public Ojo Ojo { get; set; }               // OD / OI
        public int Denominador { get; set; }       // 10..200 (el 20/ es implícito)
    }
}
