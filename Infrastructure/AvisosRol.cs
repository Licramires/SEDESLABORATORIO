using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using SEDESLABORATORIO.Models;

namespace SEDESLABORATORIO.Infrastructure;

public sealed class AvisosRol(IAvisosDatos db) : IAvisosRol
{
    public async Task AvisarRol(RolUsuario rol, string mensaje)
    {
        foreach (var id in await db.Usuarios.Where(u => u.Activo && u.Rol == rol).Select(u => u.Id).ToListAsync())
            db.Notificaciones.Add(new Notificacion { UsuarioId = id, Mensaje = mensaje });
    }
}
