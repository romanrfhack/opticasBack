using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Optica.Domain.Entities;

namespace Optica.Infrastructure.Persistence.Config
{
    public class HistoriaPagoConfig : IEntityTypeConfiguration<HistoriaPago>
    {
        public void Configure(EntityTypeBuilder<HistoriaPago> b)
        {
            b.ToTable("HistoriaPagos");
            b.HasKey(x => x.Id);

            b.HasOne(x => x.Visita)
                .WithMany(v => v.Pagos)
                .HasForeignKey(x => x.VisitaId);

            // 🔹 Monto en 12,2
            b.Property(x => x.Monto).HasPrecision(12, 2);

            b.Property(x => x.Autorizacion).HasMaxLength(60);
            b.Property(x => x.Nota).HasMaxLength(300);

            b.HasIndex(x => new { x.VisitaId, x.Fecha });
        }
    }

}
