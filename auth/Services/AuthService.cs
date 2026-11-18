using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Auth.Models;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;

namespace SEDESLABORATORIO.Auth.Services;

public sealed class AuthService : IAuthService
{
    private readonly IAutenticacionDatos _dbContext;
    private readonly IPasswordHasher<Usuario> _passwordHasher;

    public AuthService(IAutenticacionDatos dbContext, IPasswordHasher<Usuario> passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthResult> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _dbContext.Usuarios
            .SingleOrDefaultAsync(usuario => usuario.Email.ToLower() == normalizedEmail, cancellationToken);

        if (user is null || !user.Activo)
        {
            return new AuthResult(false, Error: "El correo o la contraseña no son válidos.");
        }

        PasswordVerificationResult passwordResult;
        try { passwordResult = _passwordHasher.VerifyHashedPassword(user, user.Password, password); }
        catch (FormatException) { return new AuthResult(false, Error: "El correo o la contraseña no son válidos."); }
        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return new AuthResult(false, Error: "El correo o la contraseña no son válidos.");
        }

        if (passwordResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.Password = _passwordHasher.HashPassword(user, password);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return new AuthResult(true, user);
    }
}
