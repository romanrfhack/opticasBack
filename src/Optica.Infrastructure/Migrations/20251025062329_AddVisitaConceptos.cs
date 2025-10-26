using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Optica.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitaConceptos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VisitaConceptos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VisitaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Concepto = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", precision: 12, scale: 2, nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioNombre = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SucursalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimestampUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitaConceptos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisitaConceptos_Visitas_VisitaId",
                        column: x => x.VisitaId,
                        principalTable: "Visitas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VisitaConceptos_SucursalId_VisitaId",
                table: "VisitaConceptos",
                columns: new[] { "SucursalId", "VisitaId" });

            migrationBuilder.CreateIndex(
                name: "IX_VisitaConceptos_VisitaId",
                table: "VisitaConceptos",
                column: "VisitaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VisitaConceptos");
        }
    }
}
