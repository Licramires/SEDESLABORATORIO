using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SEDESLABORATORIO.Data.Migrations
{
    /// <inheritdoc />
    public partial class RequirementSectionsAndPermissionEditor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Eliminado",
                table: "Requisitos",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Orden",
                table: "Requisitos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SeccionId",
                table: "Requisitos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "PermisosRol",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CambiosPermisos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Rol = table.Column<int>(type: "INTEGER", nullable: false),
                    Clave = table.Column<string>(type: "TEXT", nullable: false),
                    Habilitado = table.Column<bool>(type: "INTEGER", nullable: false),
                    Actor = table.Column<string>(type: "TEXT", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CambiosPermisos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SeccionesRequisitos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false),
                    Activa = table.Column<bool>(type: "INTEGER", nullable: false),
                    Eliminada = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeccionesRequisitos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Requisitos_SeccionId",
                table: "Requisitos",
                column: "SeccionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Requisitos_SeccionesRequisitos_SeccionId",
                table: "Requisitos",
                column: "SeccionId",
                principalTable: "SeccionesRequisitos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requisitos_SeccionesRequisitos_SeccionId",
                table: "Requisitos");

            migrationBuilder.DropTable(
                name: "CambiosPermisos");

            migrationBuilder.DropTable(
                name: "SeccionesRequisitos");

            migrationBuilder.DropIndex(
                name: "IX_Requisitos_SeccionId",
                table: "Requisitos");

            migrationBuilder.DropColumn(
                name: "Eliminado",
                table: "Requisitos");

            migrationBuilder.DropColumn(
                name: "Orden",
                table: "Requisitos");

            migrationBuilder.DropColumn(
                name: "SeccionId",
                table: "Requisitos");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "PermisosRol");
        }
    }
}
