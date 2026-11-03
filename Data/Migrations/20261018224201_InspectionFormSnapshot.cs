using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SEDESLABORATORIO.Data.Migrations
{
    /// <inheritdoc />
    public partial class InspectionFormSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DiasSubsanacion",
                table: "Inspecciones",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PlantillaPdf",
                table: "Inspecciones",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PlantillaVersion",
                table: "Inspecciones",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
            // Solo se reconstruye la información disponible; no se inventan fechas ni aprobaciones individuales.
            migrationBuilder.Sql("""
                UPDATE Solicitudes SET PrimerEnvio = FechaEnvio;
                UPDATE Solicitudes SET RequisitosJson = COALESCE((
                    SELECT json_group_array(json_object('Id', Id, 'Nombre', Nombre, 'Obligatorio', json(CASE WHEN Obligatorio THEN 'true' ELSE 'false' END), 'Version', Version, 'PlantillaPdf', PlantillaPdf))
                    FROM Requisitos WHERE Activo = 1), '[]'), RequisitosReconstruidos = 1
                    WHERE Estado <> 'borrador';
                UPDATE Documentos SET NombreRequisito = COALESCE((SELECT Nombre FROM Requisitos WHERE Id = Documentos.RequisitoId), 'Documento complementario heredado');
                UPDATE Documentos SET VersionArchivo = (SELECT COUNT(*) FROM Documentos d WHERE d.SolicitudId = Documentos.SolicitudId AND d.RequisitoId = Documentos.RequisitoId AND d.Id <= Documentos.Id)
                    WHERE RequisitoId <> 0;
                UPDATE Documentos SET Actual = 0 WHERE RequisitoId <> 0 AND Id <> (SELECT MAX(d.Id) FROM Documentos d WHERE d.SolicitudId = Documentos.SolicitudId AND d.RequisitoId = Documentos.RequisitoId);
                UPDATE Documentos SET EstadoRevision = 'aprobacion_global_heredada', Observacion = 'La etapa del trámite acredita revisión global previa; no se registró una decisión individual.'
                    WHERE Actual = 1 AND SolicitudId IN (SELECT Id FROM Solicitudes WHERE Estado IN ('aprobado','inspeccion','revision_acta','revision_legal','habilitado'));
                UPDATE Solicitudes SET OrigenObservacion = CASE
                    WHEN (SELECT Detalle FROM HistorialTramites h WHERE h.SolicitudId = Solicitudes.Id ORDER BY h.Fecha DESC,h.Id DESC LIMIT 1) LIKE 'Observaciones legales:%' THEN 'legal'
                    WHEN EXISTS (SELECT 1 FROM Inspecciones i WHERE i.SolicitudId = Solicitudes.Id AND i.Estado = 'validada' AND i.Resultado = 'observado') THEN 'campo'
                    ELSE 'documental' END WHERE Estado = 'observado';
                UPDATE Solicitudes SET MotivoRechazo = CASE WHEN EXISTS (SELECT 1 FROM Inspecciones i WHERE i.SolicitudId = Solicitudes.Id AND i.Estado = 'validada' AND i.Resultado = 'rechazado') THEN 'campo' ELSE 'documental' END WHERE Estado = 'rechazado';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiasSubsanacion",
                table: "Inspecciones");

            migrationBuilder.DropColumn(
                name: "PlantillaPdf",
                table: "Inspecciones");

            migrationBuilder.DropColumn(
                name: "PlantillaVersion",
                table: "Inspecciones");
        }
    }
}
