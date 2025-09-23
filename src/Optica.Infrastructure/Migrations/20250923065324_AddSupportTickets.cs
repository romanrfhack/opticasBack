using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Optica.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agudezas_Historias_VisitaId",
                table: "Agudezas");

            migrationBuilder.DropForeignKey(
                name: "FK_HistoriaPagos_Historias_VisitaId",
                table: "HistoriaPagos");

            migrationBuilder.DropForeignKey(
                name: "FK_Historias_Pacientes_PacienteId",
                table: "Historias");

            migrationBuilder.DropForeignKey(
                name: "FK_PrescripcionesLenteContacto_Historias_VisitaId",
                table: "PrescripcionesLenteContacto");

            migrationBuilder.DropForeignKey(
                name: "FK_PrescripcionesMaterial_Historias_VisitaId",
                table: "PrescripcionesMaterial");

            migrationBuilder.DropForeignKey(
                name: "FK_RxMediciones_Historias_VisitaId",
                table: "RxMediciones");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Historias",
                table: "Historias");

            migrationBuilder.RenameTable(
                name: "Historias",
                newName: "Visitas");

            migrationBuilder.RenameIndex(
                name: "IX_Historias_PacienteId",
                table: "Visitas",
                newName: "IX_Visitas_PacienteId");

            migrationBuilder.AddColumn<Guid>(
                name: "HistoriaClinicaVisitaId",
                table: "Visitas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Visitas",
                table: "Visitas",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "SupportTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Asunto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Mensaje = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTickets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Visitas_HistoriaClinicaVisitaId",
                table: "Visitas",
                column: "HistoriaClinicaVisitaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Agudezas_Visitas_VisitaId",
                table: "Agudezas",
                column: "VisitaId",
                principalTable: "Visitas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HistoriaPagos_Visitas_VisitaId",
                table: "HistoriaPagos",
                column: "VisitaId",
                principalTable: "Visitas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PrescripcionesLenteContacto_Visitas_VisitaId",
                table: "PrescripcionesLenteContacto",
                column: "VisitaId",
                principalTable: "Visitas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PrescripcionesMaterial_Visitas_VisitaId",
                table: "PrescripcionesMaterial",
                column: "VisitaId",
                principalTable: "Visitas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RxMediciones_Visitas_VisitaId",
                table: "RxMediciones",
                column: "VisitaId",
                principalTable: "Visitas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Visitas_Pacientes_PacienteId",
                table: "Visitas",
                column: "PacienteId",
                principalTable: "Pacientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Visitas_Visitas_HistoriaClinicaVisitaId",
                table: "Visitas",
                column: "HistoriaClinicaVisitaId",
                principalTable: "Visitas",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agudezas_Visitas_VisitaId",
                table: "Agudezas");

            migrationBuilder.DropForeignKey(
                name: "FK_HistoriaPagos_Visitas_VisitaId",
                table: "HistoriaPagos");

            migrationBuilder.DropForeignKey(
                name: "FK_PrescripcionesLenteContacto_Visitas_VisitaId",
                table: "PrescripcionesLenteContacto");

            migrationBuilder.DropForeignKey(
                name: "FK_PrescripcionesMaterial_Visitas_VisitaId",
                table: "PrescripcionesMaterial");

            migrationBuilder.DropForeignKey(
                name: "FK_RxMediciones_Visitas_VisitaId",
                table: "RxMediciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Visitas_Pacientes_PacienteId",
                table: "Visitas");

            migrationBuilder.DropForeignKey(
                name: "FK_Visitas_Visitas_HistoriaClinicaVisitaId",
                table: "Visitas");

            migrationBuilder.DropTable(
                name: "SupportTickets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Visitas",
                table: "Visitas");

            migrationBuilder.DropIndex(
                name: "IX_Visitas_HistoriaClinicaVisitaId",
                table: "Visitas");

            migrationBuilder.DropColumn(
                name: "HistoriaClinicaVisitaId",
                table: "Visitas");

            migrationBuilder.RenameTable(
                name: "Visitas",
                newName: "Historias");

            migrationBuilder.RenameIndex(
                name: "IX_Visitas_PacienteId",
                table: "Historias",
                newName: "IX_Historias_PacienteId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Historias",
                table: "Historias",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Agudezas_Historias_VisitaId",
                table: "Agudezas",
                column: "VisitaId",
                principalTable: "Historias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HistoriaPagos_Historias_VisitaId",
                table: "HistoriaPagos",
                column: "VisitaId",
                principalTable: "Historias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Historias_Pacientes_PacienteId",
                table: "Historias",
                column: "PacienteId",
                principalTable: "Pacientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PrescripcionesLenteContacto_Historias_VisitaId",
                table: "PrescripcionesLenteContacto",
                column: "VisitaId",
                principalTable: "Historias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PrescripcionesMaterial_Historias_VisitaId",
                table: "PrescripcionesMaterial",
                column: "VisitaId",
                principalTable: "Historias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RxMediciones_Historias_VisitaId",
                table: "RxMediciones",
                column: "VisitaId",
                principalTable: "Historias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
