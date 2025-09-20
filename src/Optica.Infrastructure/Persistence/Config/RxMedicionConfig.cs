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
    public class RxMedicionConfig : IEntityTypeConfiguration<RxMedicion>
    {
        public void Configure(EntityTypeBuilder<RxMedicion> b)
        {
            b.ToTable("RxMediciones");
            b.HasKey(x => x.Id);

            b.HasOne(x => x.Visita)
                .WithMany(v => v.Rx)
                .HasForeignKey(x => x.VisitaId);

            // 🔹 Decimales (si no usas convención global)
            b.Property(x => x.Esf).HasPrecision(6, 2);
            b.Property(x => x.Cyl).HasPrecision(6, 2);
            b.Property(x => x.Add).HasPrecision(6, 2);
            b.Property(x => x.AltOblea).HasPrecision(6, 2);

            // D.I.P. como texto (para aceptar “55-70”)
            b.Property(x => x.Dip).HasMaxLength(50);

            // 🔹 Único por Visita + Ojo + Distancia → 4 filas
            b.HasIndex(x => new { x.VisitaId, x.Ojo, x.Distancia }).IsUnique();
        }
    }

}
