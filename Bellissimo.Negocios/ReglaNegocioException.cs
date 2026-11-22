namespace Bellissimo.Negocios;

public class ReglaNegocioException : Exception
{
    public ReglaNegocioException(string mensaje)
        : base(mensaje)
    {
    }

    public ReglaNegocioException(string mensaje, Exception interna)
        : base(mensaje, interna)
    {
    }
}
