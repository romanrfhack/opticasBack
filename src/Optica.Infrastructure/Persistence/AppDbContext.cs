using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Optica.Domain.Entities;
using Optica.Domain.Enums;
using Optica.Infrastructure.Identity;

namespace Optica.Infrastructure.Persistence;
public sealed class AppDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Sucursal> Sucursales => Set<Sucursal>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Inventario> Inventarios => Set<Inventario>();
    public DbSet<InventarioMovimiento> Movimientos => Set<InventarioMovimiento>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Paciente> Pacientes => Set<Paciente>();
    //public DbSet<HistoriaClinicaVisita> Historias => Set<HistoriaClinicaVisita>();
    public DbSet<AgudezaVisual> Agudezas => Set<AgudezaVisual>();
    public DbSet<RxMedicion> RxMediciones => Set<RxMedicion>();
    public DbSet<Material> Materiales => Set<Material>();
    public DbSet<PrescripcionMaterial> PrescripcionesMaterial => Set<PrescripcionMaterial>();
    public DbSet<PrescripcionLenteContacto> PrescripcionesLenteContacto => Set<PrescripcionLenteContacto>();
    public DbSet<HistoriaPago> HistoriaPagos => Set<HistoriaPago>();
    
    public DbSet<HistoriaClinicaVisita> Visitas => Set<HistoriaClinicaVisita>();


    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Sucursal>(cfg =>
        {
            cfg.ToTable("Sucursales");
            cfg.HasKey(x => x.Id);
            cfg.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
        });

        b.Entity<Producto>(cfg =>
        {
            cfg.ToTable("Productos");
            cfg.HasKey(x => x.Id);
            cfg.Property(x => x.Sku).HasMaxLength(60).IsRequired();
            cfg.Property(x => x.Nombre).HasMaxLength(200).IsRequired();
            cfg.Property(x => x.Categoria).HasConversion<byte>();
            cfg.HasIndex(x => x.Sku).IsUnique();
        });

        b.Entity<Inventario>(cfg =>
        {
            cfg.ToTable("Inventarios");
            cfg.HasKey(x => x.Id);
            cfg.HasIndex(x => new { x.ProductoId, x.SucursalId }).IsUnique();
            cfg.HasOne(x => x.Producto).WithMany(p => p.Inventarios).HasForeignKey(x => x.ProductoId);
            cfg.HasOne(x => x.Sucursal).WithMany().HasForeignKey(x => x.SucursalId);
        });

        b.Entity<InventarioMovimiento>(cfg =>
        {
            cfg.ToTable("InventarioMovimientos");
            cfg.HasKey(x => x.Id);
            cfg.Property(x => x.Tipo).HasConversion<byte>();
            cfg.Property(x => x.Cantidad).IsRequired();
            cfg.Property(x => x.Motivo).HasMaxLength(300);
            cfg.HasOne(x => x.Producto).WithMany().HasForeignKey(x => x.ProductoId);
            cfg.HasIndex(x => x.Fecha);
        });

        b.Entity<AppUser>(cfg => { cfg.Property(x => x.FullName).HasMaxLength(120); });
        b.Entity<RefreshToken>(cfg => { cfg.ToTable("AuthRefreshTokens"); cfg.HasKey(x => x.Id); cfg.HasIndex(x => x.Token).IsUnique(); });

        b.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
    // 1) Aplica todas tus IEntityTypeConfiguration<T> automáticamente
    //protected override void OnModelCreating(ModelBuilder modelBuilder)
    //{
    //    base.OnModelCreating(modelBuilder);
    //    modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    //}

    // 2) Convención global para decimales -> decimal(12,2)
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(12, 2);
        configurationBuilder.Properties<decimal?>().HavePrecision(12, 2);
    }
}
