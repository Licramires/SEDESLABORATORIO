using System.Security.Cryptography;
using System.Text;

namespace SEDESLABORATORIO.Infrastructure;

public static class RevisionServicios
{
    public static string De(string servicios) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(servicios)));
}
