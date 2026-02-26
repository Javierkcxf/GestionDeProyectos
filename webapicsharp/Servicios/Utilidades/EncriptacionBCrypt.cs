
using BCrypt.Net;
using System;
namespace webapicsharp.Servicios.Utilidades
{
    public static class EncriptacionBCrypt
    {
        private const int CostoPorDefecto = 12;
        public static string Encriptar(string valorOriginal, int costo = CostoPorDefecto)
        {
            if (string.IsNullOrWhiteSpace(valorOriginal))
                throw new ArgumentException("El valor a encriptar no puede estar vacío.", nameof(valorOriginal));
            if (costo < 4 || costo > 31)
                throw new ArgumentOutOfRangeException(nameof(costo), costo,
                    "El costo de BCrypt debe estar entre 4 y 31. Recomendado: 10-15.");
            try
            {
                return BCrypt.Net.BCrypt.HashPassword(valorOriginal, costo);
            }
            catch (Exception excepcion)
            {
                throw new InvalidOperationException(
                    $"Error al generar hash BCrypt con costo {costo}: {excepcion.Message}",
                    excepcion
                );
            }
        }
        public static bool Verificar(string valorOriginal, string hashExistente)
        {
            if (string.IsNullOrWhiteSpace(valorOriginal))
                throw new ArgumentException("El valor a verificar no puede estar vacío.", nameof(valorOriginal));
            if (string.IsNullOrWhiteSpace(hashExistente))
                throw new ArgumentException("El hash existente no puede estar vacío.", nameof(hashExistente));
            try
            {
                return BCrypt.Net.BCrypt.Verify(valorOriginal, hashExistente);
            }
            catch (Exception)
            {
                return false;
            }
        }
        public static bool NecesitaReHasheo(string hashExistente, int costoDeseado = CostoPorDefecto)
        {
            if (string.IsNullOrWhiteSpace(hashExistente))
                return true;
            try
            {
                if (hashExistente.Length >= 7 && hashExistente.StartsWith("$2"))
                {
                    string costoParte = hashExistente.Substring(4, 2);
                    if (int.TryParse(costoParte, out int costoActual))
                    {
                        return costoActual < costoDeseado;
                    }
                }
                return true;
            }
            catch
            {
                return true;
            }
        }
    }
}
