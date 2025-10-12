using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Optica.Application.Productos.Dtos
{
    public class ProductArmazonDto
    {
        public Guid Id { get; set; }
        public string Sku { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string Categoria { get; set; } = null!;
        public bool Activo { get; set; }
        public int Stock { get; set; }
        public bool EnSucursalActiva { get; set; }
        public List<SucursalStockDto> SucursalesConStock { get; set; } = new();

        public ProductArmazonDto(Guid id, string sku, string nombre, string categoria, bool activo,
            int stock, bool enSucursalActiva)
        {
            Id = id;
            Sku = sku;
            Nombre = nombre;
            Categoria = categoria;
            Activo = activo;
            Stock = stock;
            EnSucursalActiva = enSucursalActiva;
        }
    }

    public class SucursalStockDto
    {
        public Guid SucursalId { get; set; }
        public string NombreSucursal { get; set; } = null!;
        public int Stock { get; set; }
    }
}
