using System.Security.Cryptography;
using Bellissimo.Datos;

namespace Bellissimo.Negocios;

// Hash de la clave personal del checador (RN16: nunca texto plano).
// Formato guardado en empleado.clave_hash (VARCHAR 255):
//   PBKDF2-SHA256$<iteraciones>$<sal base64>$<hash base64>
internal static class ClaveHasher
{
    private const int Iteraciones = 100_000;
    private const int TamanoSal = 16;
    private const int TamanoHash = 32;

    public static string Generar(string clave)
    {
        byte[] sal = RandomNumberGenerator.GetBytes(TamanoSal);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            clave, sal, Iteraciones, HashAlgorithmName.SHA256, TamanoHash);

        return $"{PersonalDatos.PrefijoHash}{Iteraciones}${Convert.ToBase64String(sal)}${Convert.ToBase64String(hash)}";
    }

    public static bool EsHashValido(string? claveHash)
        => claveHash is not null
           && claveHash.StartsWith(PersonalDatos.PrefijoHash, StringComparison.Ordinal)
           && claveHash.Split('$').Length == 4;

    public static bool Verificar(string clave, string? claveHash)
    {
        if (!EsHashValido(claveHash))
            return false;

        string[] partes = claveHash!.Split('$');

        try
        {
            int iteraciones = int.Parse(partes[1]);
            byte[] sal = Convert.FromBase64String(partes[2]);
            byte[] esperado = Convert.FromBase64String(partes[3]);

            byte[] calculado = Rfc2898DeriveBytes.Pbkdf2(
                clave, sal, iteraciones, HashAlgorithmName.SHA256, esperado.Length);

            return CryptographicOperations.FixedTimeEquals(calculado, esperado);
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException)
        {
            return false;
        }
    }
}
