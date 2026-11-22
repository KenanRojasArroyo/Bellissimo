namespace Bellissimo.Negocios;

public class ReglaNegocioException : Exception
{
    public ReglaNegocioException(string mensaje)
        : base(mensaje)
    {
    }
}
