namespace SEDESLABORATORIO.Infrastructure;

public static class Coordenadas
{
    public static bool Validar(decimal latitud, decimal longitud)
        => latitud is >= -90 and <= 90 && longitud is >= -180 and <= 180;
}
