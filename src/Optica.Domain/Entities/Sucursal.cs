namespace Optica.Domain.Entities;

public sealed class Sucursal
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = null!;
    public bool Activa { get; set; } = true;
}