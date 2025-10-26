using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using Optica.Domain.Entities;
using Optica.Infrastructure.Identity;

using System.Reflection.Emit;

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
    public DbSet<VisitaStatusHistory> VisitaStatusHistory => Set<VisitaStatusHistory>();
    public DbSet<VisitaConcepto> VisitaConceptos => Set<VisitaConcepto>();


    // CREATE INDEX IX_Visitas_SucursalId_Fecha ON dbo.Visitas(SucursalId, Fecha DESC);
    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // ---------- Sucursal ----------
        b.Entity<Sucursal>(cfg =>
        {
            cfg.ToTable("Sucursales");
            cfg.HasKey(x => x.Id);
            cfg.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
        });

        // ---------- Producto ----------
        b.Entity<Producto>(cfg =>
        {
            cfg.ToTable("Productos");
            cfg.HasKey(x => x.Id);
            cfg.Property(x => x.Sku).HasMaxLength(60).IsRequired();
            cfg.Property(x => x.Nombre).HasMaxLength(200).IsRequired();
            cfg.Property(x => x.Categoria).HasConversion<byte>();
            cfg.HasIndex(x => x.Sku).IsUnique();
        });

        // ---------- Inventario ----------
        b.Entity<Inventario>(cfg =>
        {
            cfg.ToTable("Inventarios");
            cfg.HasKey(x => x.Id);
            cfg.HasIndex(x => new { x.ProductoId, x.SucursalId }).IsUnique();
            cfg.HasOne(x => x.Producto)
               .WithMany(p => p.Inventarios)
               .HasForeignKey(x => x.ProductoId)
               .OnDelete(DeleteBehavior.Restrict);

            cfg.HasOne(x => x.Sucursal)
               .WithMany()
               .HasForeignKey(x => x.SucursalId)
               .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- InventarioMovimiento ----------
        b.Entity<InventarioMovimiento>(cfg =>
        {
            cfg.ToTable("InventarioMovimientos");
            cfg.HasKey(x => x.Id);
            cfg.Property(x => x.Tipo).HasConversion<byte>();
            cfg.Property(x => x.Cantidad).IsRequired();
            cfg.Property(x => x.Motivo).HasMaxLength(300);
            cfg.HasOne(x => x.Producto)
               .WithMany()
               .HasForeignKey(x => x.ProductoId)
               .OnDelete(DeleteBehavior.Restrict);
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

            // Relación con Sucursal (sin cascada)
            cfg.HasOne(p => p.SucursalAlta)
               .WithMany()
               .HasForeignKey(p => p.SucursalIdAlta)
               .OnDelete(DeleteBehavior.Restrict);

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

            // Columnas normalizadas
            cfg.Property(x => x.NombreNormalized)
                .HasMaxLength(200)
                .HasComputedColumnSql("UPPER(LTRIM(RTRIM([Nombre])))", stored: true);

            cfg.Property(x => x.TelefonoNormalized)
                .HasMaxLength(30)
                .HasComputedColumnSql("LTRIM(RTRIM([Telefono]))", stored: true);

            // Índice único por nombre + teléfono
            cfg.HasIndex(x => new { x.NombreNormalized, x.TelefonoNormalized })
                .IsUnique()
                .HasFilter("[Nombre] IS NOT NULL AND [Telefono] IS NOT NULL AND [Telefono] <> ''");
        });

        // ---------- HistoriaClinicaVisita (Visitas) ----------
        b.Entity<HistoriaClinicaVisita>(cfg =>
        {
            cfg.ToTable("Visitas");
            cfg.HasKey(x => x.Id);

            // Relación con Sucursal (sin cascada)
            cfg.HasOne(v => v.Sucursal)
               .WithMany()
               .HasForeignKey(v => v.SucursalId)
               .OnDelete(DeleteBehavior.Restrict);

            // Relación con Paciente (sin cascada)
            cfg.HasOne(v => v.Paciente)
               .WithMany(p => p.Visitas)
               .HasForeignKey(v => v.PacienteId)
               .OnDelete(DeleteBehavior.Restrict);

            // Si tienes relación con Usuario (quien atendió), también sin cascada
            // cfg.HasOne(v => v.Usuario)
            //    .WithMany()
            //    .HasForeignKey(v => v.UsuarioId)
            //    .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- VisitaStatusHistory ----------
        b.Entity<VisitaStatusHistory>(e =>
        {
            e.ToTable("VisitaStatusHistory");
            e.HasKey(x => x.Id);
            e.Property(x => x.FromStatus).HasMaxLength(80).IsRequired();
            e.Property(x => x.ToStatus).HasMaxLength(80).IsRequired();
            e.Property(x => x.UsuarioNombre).HasMaxLength(150).IsRequired();
            e.Property(x => x.Observaciones).HasMaxLength(1000);
            e.Property(x => x.LabTipo).HasMaxLength(20);
            e.Property(x => x.LabNombre).HasMaxLength(150);
            e.HasIndex(x => new { x.VisitaId, x.TimestampUtc });

            //// Relación con Visita (sí puede tener cascada)
            //e.HasOne(h => h.Visita)
            // .WithMany(v => v.StatusHistory)
            // .HasForeignKey(h => h.VisitaId)
            // .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<VisitaConcepto>(e =>
        {
            e.ToTable("VisitaConceptos");
            e.HasKey(x => x.Id);
            e.Property(x => x.Monto).HasColumnType("decimal(18,2)");
            e.Property(x => x.Concepto).HasMaxLength(128).IsRequired();
            e.Property(x => x.UsuarioNombre).HasMaxLength(128).IsRequired();
            e.Property(x => x.Observaciones).HasMaxLength(1024);

            e.HasIndex(x => x.VisitaId);
            e.HasIndex(x => new { x.SucursalId, x.VisitaId });
            e.HasOne(x => x.Visita)
                .WithMany(v => v.Conceptos)  
                .HasForeignKey(x => x.VisitaId);
        });


        b.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(12, 2);
        configurationBuilder.Properties<decimal?>().HavePrecision(12, 2);
    }
}
