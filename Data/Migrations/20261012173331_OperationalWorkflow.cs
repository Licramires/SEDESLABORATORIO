using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SEDESLABORATORIO.Data.Migrations
{
    /// <inheritdoc />
    public partial class OperationalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Normaliza el valor histórico sin perder trámites del Sprint 1.
            migrationBuilder.Sql("UPDATE Solicitudes SET Estado = 'en_revision' WHERE Estado = 'enrevision';");
            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Usuarios",
                type: "TEXT",
                nullable: false,
                collation: "NOCASE",
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Usuarios",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "SelloSeguridad",
                table: "Usuarios",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "EsRenovacion",
                table: "Solicitudes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaEnvio",
                table: "Solicitudes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirmaLegalPdf",
                table: "Solicitudes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "LaboratorioId",
                table: "Solicitudes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Municipio",
                table: "Solicitudes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Solicitudes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Municipio",
                table: "Laboratorios",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PropietarioId",
                table: "Laboratorios",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VigenciaHasta",
                table: "Laboratorios",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HistorialTramites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SolicitudId = table.Column<int>(type: "INTEGER", nullable: false),
                    ActorId = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Detalle = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialTramites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialTramites_Solicitudes_SolicitudId",
                        column: x => x.SolicitudId,
                        principalTable: "Solicitudes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HistorialTramites_Usuarios_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Inspecciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SolicitudId = table.Column<int>(type: "INTEGER", nullable: false),
                    SupervisorId = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", nullable: false),
                    Resultado = table.Column<string>(type: "TEXT", nullable: false),
                    Acta = table.Column<string>(type: "TEXT", nullable: false),
                    ActaPdf = table.Column<string>(type: "TEXT", nullable: false),
                    Citacion = table.Column<string>(type: "TEXT", nullable: false),
                    PlazoSubsanacion = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inspecciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inspecciones_Solicitudes_SolicitudId",
                        column: x => x.SolicitudId,
                        principalTable: "Solicitudes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Inspecciones_Usuarios_SupervisorId",
                        column: x => x.SupervisorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notificaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Mensaje = table.Column<string>(type: "TEXT", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Leida = table.Column<bool>(type: "INTEGER", nullable: false),
                    Clave = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notificaciones_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Requisitos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Obligatorio = table.Column<bool>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Requisitos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_LaboratorioId",
                table: "Solicitudes",
                column: "LaboratorioId");

            migrationBuilder.CreateIndex(
                name: "IX_Laboratorios_PropietarioId",
                table: "Laboratorios",
                column: "PropietarioId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialTramites_ActorId",
                table: "HistorialTramites",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialTramites_SolicitudId",
                table: "HistorialTramites",
                column: "SolicitudId");

            migrationBuilder.CreateIndex(
                name: "IX_Inspecciones_SolicitudId",
                table: "Inspecciones",
                column: "SolicitudId");

            migrationBuilder.CreateIndex(
                name: "IX_Inspecciones_SupervisorId",
                table: "Inspecciones",
                column: "SupervisorId");

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_Clave",
                table: "Notificaciones",
                column: "Clave",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_UsuarioId",
                table: "Notificaciones",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Laboratorios_Usuarios_PropietarioId",
                table: "Laboratorios",
                column: "PropietarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Solicitudes_Laboratorios_LaboratorioId",
                table: "Solicitudes",
                column: "LaboratorioId",
                principalTable: "Laboratorios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Solicitudes SET Estado = 'enrevision' WHERE Estado = 'en_revision';");
            migrationBuilder.DropForeignKey(
                name: "FK_Laboratorios_Usuarios_PropietarioId",
                table: "Laboratorios");

            migrationBuilder.DropForeignKey(
                name: "FK_Solicitudes_Laboratorios_LaboratorioId",
                table: "Solicitudes");

            migrationBuilder.DropTable(
                name: "HistorialTramites");

            migrationBuilder.DropTable(
                name: "Inspecciones");

            migrationBuilder.DropTable(
                name: "Notificaciones");

            migrationBuilder.DropTable(
                name: "Requisitos");

            migrationBuilder.DropIndex(
                name: "IX_Solicitudes_LaboratorioId",
                table: "Solicitudes");

            migrationBuilder.DropIndex(
                name: "IX_Laboratorios_PropietarioId",
                table: "Laboratorios");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "SelloSeguridad",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "EsRenovacion",
                table: "Solicitudes");

            migrationBuilder.DropColumn(
                name: "FechaEnvio",
                table: "Solicitudes");

            migrationBuilder.DropColumn(
                name: "FirmaLegalPdf",
                table: "Solicitudes");

            migrationBuilder.DropColumn(
                name: "LaboratorioId",
                table: "Solicitudes");

            migrationBuilder.DropColumn(
                name: "Municipio",
                table: "Solicitudes");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Solicitudes");

            migrationBuilder.DropColumn(
                name: "Municipio",
                table: "Laboratorios");

            migrationBuilder.DropColumn(
                name: "PropietarioId",
                table: "Laboratorios");

            migrationBuilder.DropColumn(
                name: "VigenciaHasta",
                table: "Laboratorios");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Usuarios",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldCollation: "NOCASE");
        }
    }
}

