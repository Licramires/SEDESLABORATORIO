using SEDESLABORATORIO.Auth.Models;

namespace SEDESLABORATORIO.Auth.Services;

public interface IAuthService
{
    Task<AuthResult> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default);
}
