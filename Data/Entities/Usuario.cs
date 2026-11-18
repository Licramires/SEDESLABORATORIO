namespace SEDESLABORATORIO.Data.Entities;

public class Usuario
{
    public int Version { get; set; } = 1;
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public RolUsuario Rol { get; set; }
    public bool Activo { get; set; } = true;
    public string SelloSeguridad { get; set; } = Guid.NewGuid().ToString("N");
    public ICollection<Solicitud> Solicitudes { get; set; } = [];
}

public enum RolUsuario
{
    Propietario,
    Coordinador,
    Supervisor,
    Gerente,
    Administrador,
    Abogado
}
