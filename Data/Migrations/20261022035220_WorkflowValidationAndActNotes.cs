using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SEDESLABORATORIO.Data.Migrations
{
    /// <inheritdoc />
    public partial class WorkflowValidationAndActNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Inspecciones_SupervisorId",
                table: "Inspecciones");

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Usuarios",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "NotasAdicionales",
                table: "Inspecciones",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Inspecciones_SupervisorId_Fecha",
                table: "Inspecciones",
                columns: new[] { "SupervisorId", "Fecha" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Inspecciones_SupervisorId_Fecha",
                table: "Inspecciones");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "NotasAdicionales",
                table: "Inspecciones");

            migrationBuilder.CreateIndex(
                name: "IX_Inspecciones_SupervisorId",
                table: "Inspecciones",
                column: "SupervisorId");
        }
    }
}
