namespace SEDESLABORATORIO.Models;

public sealed record OperacionFallidaModel(string Mensaje, bool Conflicto, string RutaRetorno, string AccionRetorno);
