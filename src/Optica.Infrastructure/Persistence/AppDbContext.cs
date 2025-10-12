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
    public DbSet<AgudezaVisual> Agudezas => Set<AgudezaVisual>();
    public DbSet<RxMedicion> RxMediciones => Set<RxMedicion>();
    public DbSet<Material> Materiales => Set<Material>();
    public DbSet<PrescripcionMaterial> PrescripcionesMaterial => Set<PrescripcionMaterial>();
    public DbSet<PrescripcionLenteContacto> PrescripcionesLenteContacto => Set<PrescripcionLenteContacto>();
    public DbSet<HistoriaPago> HistoriaPagos => Set<HistoriaPago>();
    public DbSet<HistoriaClinicaVisita> Visitas => Set<HistoriaClinicaVisita>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();

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

        // ---------- AppUser / RefreshToken ----------
        b.Entity<AppUser>(cfg =>
        {
            cfg.Property(x => x.FullName).HasMaxLength(120);
        });

        b.Entity<RefreshToken>(cfg =>
        {
            cfg.ToTable("AuthRefreshTokens");
            cfg.HasKey(x => x.Id);
            cfg.HasIndex(x => x.Token).IsUnique();
        });

        // ---------- Paciente ----------
        b.Entity<Paciente>(cfg =>
        {
            cfg.ToTable("Pacientes");
            cfg.HasKey(x => x.Id);

            // Relación con Sucursal
            cfg.HasOne(p => p.SucursalAlta)
                .WithMany()
                .HasForeignKey(p => p.SucursalIdAlta);

            // Propiedades principales
            cfg.Property(x => x.Nombre).HasMaxLength(200).IsRequired();
            cfg.Property(x => x.Telefono).HasMaxLength(30);
            cfg.Property(x => x.Ocupacion).HasMaxLength(120);
            cfg.Property(x => x.Direccion).HasMaxLength(300);

            // Auditoría
            cfg.Property(x => x.CreadoPorUsuarioId);
            cfg.Property(x => x.CreadoPorNombre).HasMaxLength(200);
            cfg.Property(x => x.CreadoPorEmail).HasMaxLength(200);

            // Fecha de registro con default UTC
            cfg.Property(x => x.FechaRegistro)
                .HasDefaultValueSql("GETUTCDATE()");

            // Columnas normalizadas persistidas
            cfg.Property(x => x.NombreNormalized)
                .HasMaxLength(200)
                .HasComputedColumnSql("UPPER(LTRIM(RTRIM([Nombre])))", stored: true);

            cfg.Property(x => x.TelefonoNormalized)
                .HasMaxLength(30)
                .HasComputedColumnSql("LTRIM(RTRIM([Telefono]))", stored: true);

            // Índice único por nombre + teléfono normalizados
            cfg.HasIndex(x => new { x.NombreNormalized, x.TelefonoNormalized })
                .IsUnique()
                .HasFilter("[Nombre] IS NOT NULL AND [Telefono] IS NOT NULL AND [Telefono] <> ''");
        });


        b.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(12, 2);
        configurationBuilder.Properties<decimal?>().HavePrecision(12, 2);
    }
}
