using Optica.Domain.Enums;

namespace Optica.Domain.Entities;
public sealed class InventarioMovimiento
{
    public Guid Id { get; set; }
    public Guid ProductoId { get; set; }
    public Guid? DesdeSucursalId { get; set; } // null en entradas
    public Guid? HaciaSucursalId { get; set; } // null en salidas
    public int Cantidad { get; set; }          // >0
    public TipoMovimiento Tipo { get; set; }
    public string? Motivo { get; set; }
    public DateTimeOffset Fecha { get; set; } = DateTimeOffset.UtcNow;

    public Producto Producto { get; set; } = null!;
}
