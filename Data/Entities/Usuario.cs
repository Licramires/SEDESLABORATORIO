namespace SEDESLABORATORIO.Data.Entities;

public class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public RolUsuario Rol { get; set; }
    public ICollection<Solicitud> Solicitudes { get; set; } = [];
}

public enum RolUsuario
{
    Propietario,
    Coordinador,
    Supervisor,
    Gerente,
    Administrador
}
