using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SEDESLABORATORIO.Data.Migrations
{
    /// <inheritdoc />
    public partial class AuditCorrections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "Solicitudes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Direccion",
                table: "Solicitudes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCierre",
                table: "Solicitudes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "InformeCierrePdf",
                table: "Solicitudes",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoRechazo",
                table: "Solicitudes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OrigenObservacion",
                table: "Solicitudes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "PrimerEnvio",
                table: "Solicitudes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiereNuevaInspeccion",
                table: "Solicitudes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RequisitosJson",
                table: "Solicitudes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "RequisitosReconstruidos",
                table: "Solicitudes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Telefono",
                table: "Solicitudes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "VigenciaOtorgadaDesde",
                table: "Solicitudes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VigenciaOtorgadaHasta",
                table: "Solicitudes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlantillaPdf",
                table: "Requisitos",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Requisitos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "Laboratorios",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "DatosVerificados",
                table: "Laboratorios",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Direccion",
                table: "Laboratorios",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Telefono",
                table: "Laboratorios",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaFin",
                table: "Inspecciones",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ListaVerificacionJson",
                table: "Inspecciones",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Programada",
                table: "Inspecciones",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "Actual",
                table: "Documentos",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "EstadoRevision",
                table: "Documentos",
                type: "TEXT",
                nullable: false,
                defaultValue: "pendiente");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCarga",
                table: "Documentos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaRevision",
                table: "Documentos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreRequisito",
                table: "Documentos",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Observacion",
                table: "Documentos",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RevisorId",
                table: "Documentos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VersionArchivo",
                table: "Documentos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "Catalogos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Clase = table.Column<string>(type: "TEXT", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    Referencia = table.Column<string>(type: "TEXT", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    ArchivoPdf = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Catalogos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Citaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SolicitudId = table.Column<int>(type: "INTEGER", nullable: false),
                    InspeccionId = table.Column<int>(type: "INTEGER", nullable: false),
                    SupervisorId = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Vencimiento = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AnticipacionDias = table.Column<int>(type: "INTEGER", nullable: false),
                    Motivo = table.Column<string>(type: "TEXT", nullable: false),
                    EvidenciaPdf = table.Column<string>(type: "TEXT", nullable: false),
                    FechaAtencion = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Citaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Citaciones_Inspecciones_InspeccionId",
                        column: x => x.InspeccionId,
                        principalTable: "Inspecciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Citaciones_Solicitudes_SolicitudId",
                        column: x => x.SolicitudId,
                        principalTable: "Solicitudes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Citaciones_Usuarios_SupervisorId",
                        column: x => x.SupervisorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_RevisorId",
                table: "Documentos",
                column: "RevisorId");

            migrationBuilder.CreateIndex(
                name: "IX_Catalogos_Clase_Nombre",
                table: "Catalogos",
                columns: new[] { "Clase", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Citaciones_InspeccionId",
                table: "Citaciones",
                column: "InspeccionId");

            migrationBuilder.CreateIndex(
                name: "IX_Citaciones_SolicitudId",
                table: "Citaciones",
                column: "SolicitudId");

            migrationBuilder.CreateIndex(
                name: "IX_Citaciones_SupervisorId",
                table: "Citaciones",
                column: "SupervisorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documentos_Usuarios_RevisorId",
                table: "Documentos",
                column: "RevisorId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);


        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documentos_Usuarios_RevisorId",
                table: "Documentos");

            migrationBuilder.DropTable(
                name: "Catalogos");

            migrationBuilder.DropTable(
                name: "Citaciones");

            migrationBuilder.DropIndex(
                name: "IX_Documentos_RevisorId",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "Solicitudes");

            migrationBuilder.DropColumn(
                name: "Direccion",
                table: "Solicitudes");

            migrationBuilder.DropColumn(
                name: "FechaCierre",
                table: "Solicitudes");

            migrationBuilder.DropColumn(
                name: "InformeCierrePdf",
                table: "Solicitudes");

            migrationBuilder.DropColumn(
                name: "MotivoRechazo",
                table: "Solicitudes");

            migrationBuilder.DropColumn(
                name: "OrigenObservacion",
                table: "Solicitudes");

            migrationBuilder.DropColumn(
                name: "PrimerEnvio",
                table: "Solicitudes");

            migrationBuilder.DropColumn(
                name: "RequiereNuevaInspeccion",
                table: "Solicitudes");

            migrationBuilder.DropColumn(
                name: "RequisitosJson",
                table: "Solicitudes");

            migrationBuilder.DropColumn(
                name: "RequisitosReconstruidos",
                table: "Solicitudes");

            migrationBuilder.DropColumn(
                name: "Telefono",
                table: "Solicitudes");

            migrationBuilder.DropColumn(
                name: "VigenciaOtorgadaDesde",
                table: "Solicitudes");

            migrationBuilder.DropColumn(
                name: "VigenciaOtorgadaHasta",
                table: "Solicitudes");

            migrationBuilder.DropColumn(
                name: "PlantillaPdf",
                table: "Requisitos");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Requisitos");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "Laboratorios");

            migrationBuilder.DropColumn(
                name: "DatosVerificados",
                table: "Laboratorios");

            migrationBuilder.DropColumn(
                name: "Direccion",
                table: "Laboratorios");

            migrationBuilder.DropColumn(
                name: "Telefono",
                table: "Laboratorios");

            migrationBuilder.DropColumn(
                name: "FechaFin",
                table: "Inspecciones");

            migrationBuilder.DropColumn(
                name: "ListaVerificacionJson",
                table: "Inspecciones");

            migrationBuilder.DropColumn(
                name: "Programada",
                table: "Inspecciones");

            migrationBuilder.DropColumn(
                name: "Actual",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "EstadoRevision",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "FechaCarga",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "FechaRevision",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "NombreRequisito",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "Observacion",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "RevisorId",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "VersionArchivo",
                table: "Documentos");
        }
    }
}
