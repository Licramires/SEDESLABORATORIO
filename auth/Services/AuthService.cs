using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Auth.Models;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;

namespace SEDESLABORATORIO.Auth.Services;

public sealed class AuthService : IAuthService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly PasswordHasher<Usuario> _passwordHasher = new();

    public AuthService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AuthResult> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _dbContext.Usuarios
            .SingleOrDefaultAsync(usuario => usuario.Email.ToLower() == normalizedEmail, cancellationToken);

        if (user is null)
        {
            return new AuthResult(false, Error: "El correo o la contraseña no son válidos.");
        }

        var passwordResult = _passwordHasher.VerifyHashedPassword(user, user.Password, password);
        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return new AuthResult(false, Error: "El correo o la contraseña no son válidos.");
        }

        return new AuthResult(true, user);
    }
}
