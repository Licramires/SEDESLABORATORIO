using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

namespace SEDESLABORATORIO.Infrastructure;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class TextoSeguroAttribute : ValidationAttribute
{
    public bool Multilinea { get; set; }
    public TextoSeguroAttribute() : base("El campo contiene caracteres de control que no se permiten.") { }
    public override bool IsValid(object? value) => value is null || value is string text && EsValido(text, Multilinea);
    public static bool EsValido(string text, bool multilinea = false) => !text.Any(c =>
        char.IsControl(c) && !(multilinea && c is '\r' or '\n' or '\t'));
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class CorreoValidoAttribute : ValidationAttribute
{
    public CorreoValidoAttribute() : base("Ingrese un correo válido, sin espacios ni nombre visible.") { }
    public override bool IsValid(object? value) => value is null || value is string text && EsDireccion(text);
    public static bool EsDireccion(string value)
    {
        var text=value.Trim();
        if(text.Length==0 || text.Any(char.IsWhiteSpace) || !TextoSeguroAttribute.EsValido(text) ||
            !MailAddress.TryCreate(text,out var address) || !address.Address.Equals(text,StringComparison.OrdinalIgnoreCase)) return false;
        var local=text[..text.LastIndexOf('@')]; var host=address.Host;
        return !local.StartsWith('.') && !local.EndsWith('.') && !local.Contains("..",StringComparison.Ordinal) &&
            host.Split('.').All(label=>label.Length is >0 and <=63 && !label.StartsWith('-') && !label.EndsWith('-') && label.All(c=>char.IsLetterOrDigit(c)||c=='-'));
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class FechaCivilAttribute : ValidationAttribute
{
    public bool SoloFecha { get; set; }
    public FechaCivilAttribute() : base("Ingrese la fecha local sin zona horaria; use solo la fecha cuando el campo no solicita hora.") { }
    public override bool IsValid(object? value) => value is null || value is DateTime date &&
        date.Kind == DateTimeKind.Unspecified && (!SoloFecha || date.TimeOfDay == TimeSpan.Zero);
}
