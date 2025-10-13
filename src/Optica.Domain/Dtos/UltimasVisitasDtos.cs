using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Optica.Domain.Entities;

namespace Optica.Domain.Dtos
{

    public sealed class UltimaVisitaDto
    {
        public Guid Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Estado { get; set; } = default!;
        public decimal? Total { get; set; }
        public decimal ACuenta { get; set; }
        public decimal Resta { get; set; }
        public PagoMiniDto? UltimoPago { get; set; }
        public List<RxMedicionDto> Rx { get; set; } = new();
    }

    public class RxMedicionDto
    {
        public Guid Id { get; set; }
        public string Ojo { get; set; } = string.Empty;
        public string Distancia { get; set; } = string.Empty;
        public decimal? Esf { get; set; }
        public decimal? Cyl { get; set; }
        public int? Eje { get; set; }
        public decimal? Add { get; set; }
        public string? Dip { get; set; }
        public decimal? AltOblea { get; set; }
    }

    public sealed class PagoMiniDto
    {
        public DateTime Fecha { get; set; }
        public decimal Monto { get; set; }
        public string Metodo { get; set; } = default!;
        public string? Autorizacion { get; set; }
        public string? Nota { get; set; }
    }

    public sealed class VisitaDetalleDto
    {
        public Guid Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Estado { get; set; } = default!;
        public decimal? Total { get; set; }
        public decimal ACuenta { get; set; }
        public decimal Resta { get; set; }

        public Guid PacienteId { get; set; }
        public string PacienteNombre { get; set; } = default!;
        public string? PacienteTelefono { get; set; }

        public List<RxMedicionDto> Rx { get; set; } = new();
        public List<AgudezaVisual> Av { get; set; } = new();

        public List<PagoMiniDto> Pagos { get; set; } = new();

        public DateTime? FechaEstimadaEntrega { get; set; }
        public DateTime? FechaRecibidaSucursal { get; set; }
        public DateTime? FechaEntregadaCliente { get; set; }

        // Si quieres mostrar también lo seleccionado:
        public List<MaterialSeleccionDto> Materiales { get; set; } = new();
        public List<LenteContactoSeleccionDto> LentesContacto { get; set; } = new();
    }

    public sealed class MaterialSeleccionDto
    {
        public Guid MaterialId { get; set; }
        public string Descripcion { get; set; } = default!;
        public string? Marca { get; set; }
        public string? Observaciones { get; set; }
    }
    public sealed class LenteContactoSeleccionDto
    {
        public string Tipo { get; set; } = default!;
        public string? Marca { get; set; }
        public string? Modelo { get; set; }
        public string? Observaciones { get; set; }
    }

    public sealed class AvDto
    {
        public string Ojo { get; set; }
        public string Condicion { get; set; }
        public int Denominador { get; set; }
    }
}
