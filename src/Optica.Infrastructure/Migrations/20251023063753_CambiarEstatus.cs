using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Optica.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CambiarEstatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventarioMovimientos_Productos_ProductoId",
                table: "InventarioMovimientos");

            migrationBuilder.DropForeignKey(
                name: "FK_Inventarios_Productos_ProductoId",
                table: "Inventarios");

            migrationBuilder.DropForeignKey(
                name: "FK_Inventarios_Sucursales_SucursalId",
                table: "Inventarios");

            migrationBuilder.DropForeignKey(
                name: "FK_Pacientes_Sucursales_SucursalIdAlta",
                table: "Pacientes");

            migrationBuilder.DropForeignKey(
                name: "FK_Visitas_Pacientes_PacienteId",
                table: "Visitas");

            migrationBuilder.CreateTable(
                name: "VisitaStatusHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VisitaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ToStatus = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SucursalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimestampUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LabTipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LabId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LabNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitaStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisitaStatusHistory_Visitas_VisitaId",
                        column: x => x.VisitaId,
                        principalTable: "Visitas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Visitas_SucursalId",
                table: "Visitas",
                column: "SucursalId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitaStatusHistory_VisitaId_TimestampUtc",
                table: "VisitaStatusHistory",
                columns: new[] { "VisitaId", "TimestampUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_InventarioMovimientos_Productos_ProductoId",
                table: "InventarioMovimientos",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Inventarios_Productos_ProductoId",
                table: "Inventarios",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Inventarios_Sucursales_SucursalId",
                table: "Inventarios",
                column: "SucursalId",
                principalTable: "Sucursales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pacientes_Sucursales_SucursalIdAlta",
                table: "Pacientes",
                column: "SucursalIdAlta",
                principalTable: "Sucursales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Visitas_Pacientes_PacienteId",
                table: "Visitas",
                column: "PacienteId",
                principalTable: "Pacientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Visitas_Sucursales_SucursalId",
                table: "Visitas",
                column: "SucursalId",
                principalTable: "Sucursales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventarioMovimientos_Productos_ProductoId",
                table: "InventarioMovimientos");

            migrationBuilder.DropForeignKey(
                name: "FK_Inventarios_Productos_ProductoId",
                table: "Inventarios");

            migrationBuilder.DropForeignKey(
                name: "FK_Inventarios_Sucursales_SucursalId",
                table: "Inventarios");

            migrationBuilder.DropForeignKey(
                name: "FK_Pacientes_Sucursales_SucursalIdAlta",
                table: "Pacientes");

            migrationBuilder.DropForeignKey(
                name: "FK_Visitas_Pacientes_PacienteId",
                table: "Visitas");

            migrationBuilder.DropForeignKey(
                name: "FK_Visitas_Sucursales_SucursalId",
                table: "Visitas");

            migrationBuilder.DropTable(
                name: "VisitaStatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_Visitas_SucursalId",
                table: "Visitas");

            migrationBuilder.AddForeignKey(
                name: "FK_InventarioMovimientos_Productos_ProductoId",
                table: "InventarioMovimientos",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Inventarios_Productos_ProductoId",
                table: "Inventarios",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Inventarios_Sucursales_SucursalId",
                table: "Inventarios",
                column: "SucursalId",
                principalTable: "Sucursales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Pacientes_Sucursales_SucursalIdAlta",
                table: "Pacientes",
                column: "SucursalIdAlta",
                principalTable: "Sucursales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Visitas_Pacientes_PacienteId",
                table: "Visitas",
                column: "PacienteId",
                principalTable: "Pacientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
