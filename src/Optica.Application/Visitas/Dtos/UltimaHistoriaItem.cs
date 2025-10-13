using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Optica.Domain.Dtos;

namespace Optica.Application.Visitas.Dtos
{
    public class UltimaHistoriaItem
    {
        public Guid Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Estado { get; set; } = string.Empty;
        public decimal? Total { get; set; }
        public decimal? ACuenta { get; set; }
        public decimal? Resta { get; set; }
        //public Guid SucursalId { get; set; }
        public string NombreSucursal { get; set; } = string.Empty;
        public string UsuarioNombre { get; set; } = string.Empty;
    }

    // DTO para respuesta detallada
    public class VisitaCompletaDto
    {
        public Guid Id { get; set; }
        public Guid PacienteId { get; set; }
        public Guid SucursalId { get; set; }
        public string NombreSucursal { get; set; } = string.Empty;
        public Guid UsuarioId { get; set; }
        public string UsuarioNombre { get; set; } = string.Empty;
        public string UsuarioEmail { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string Estado { get; set; } = string.Empty;
        public decimal? Total { get; set; }
        public decimal? ACuenta { get; set; }
        public decimal? Resta { get; set; }
        public DateTime? FechaEnvioLaboratorio { get; set; }
        public DateTime? FechaEstimadaEntrega { get; set; }
        public DateTime? FechaRecibidoSucursal { get; set; }
        public DateTime? FechaEntregaCliente { get; set; }
        public string? Observaciones { get; set; }

        // Información del paciente (solo datos básicos, no toda la entidad)
        public PacienteDto? Paciente { get; set; }
        public List<AgudezaVisualDto> Agudezas { get; set; } = new();
        public List<RxMedicionDto> Rx { get; set; } = new();
        public List<PrescripcionMaterialDto> Materiales { get; set; } = new();
        public List<PrescripcionArmazonDto> Armazones { get; set; } = new();
        public List<PrescripcionLenteContactoDto> LentesContacto { get; set; } = new();
    }

    // DTO para paciente (solo datos básicos)
    public class PacienteDto
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Edad { get; set; }
        public string Telefono { get; set; } = string.Empty;
        public string Ocupacion { get; set; } = string.Empty;
        public string? Direccion { get; set; }
    }

    // DTOs para las entidades relacionadas
    public class AgudezaVisualDto
    {
        public Guid Id { get; set; }
        public string Condicion { get; set; } = string.Empty;
        public string Ojo { get; set; } = string.Empty;
        public int Denominador { get; set; }
    }

    

    public class PrescripcionMaterialDto
    {
        public Guid Id { get; set; }
        public Guid MaterialId { get; set; }
        public string? Observaciones { get; set; }
        public MaterialDto? Material { get; set; }
    }

    public class MaterialDto
    {
        public Guid Id { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string? Marca { get; set; }
    }    

    public class PrescripcionArmazonDto
    {
        public Guid Id { get; set; }
        public Guid ProductoId { get; set; }
        public string? Observaciones { get; set; }
        public ProductoDto? Producto { get; set; }
    }

    public class ProductoDto
    {
        public Guid Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
    }

    public class PrescripcionLenteContactoDto
    {
        public Guid Id { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string? Marca { get; set; }
        public string? Modelo { get; set; }
        public string? Observaciones { get; set; }
    }
}
