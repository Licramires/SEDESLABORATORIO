using SEDESLABORATORIO.Data.Entities;

namespace SEDESLABORATORIO.Auth.Models;

public sealed record AuthResult(bool Succeeded, Usuario? User = null, string? Error = null);
