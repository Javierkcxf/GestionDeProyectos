
using Microsoft.Extensions.Configuration;     // Permite leer appsettings.*.json
using System;                                  // Para InvalidOperationException
using webapicsharp.Servicios.Abstracciones;   // Para IProveedorConexion
namespace webapicsharp.Servicios.Conexion
{
   public class ProveedorConexion : IProveedorConexion
   {
       private readonly IConfiguration _configuracion;
       public ProveedorConexion(IConfiguration configuracion)
       {
           _configuracion = configuracion ?? throw new ArgumentNullException(
               nameof(configuracion),
               "IConfiguration no puede ser null. Verificar configuración de inyección de dependencias en Program.cs."
           );
       }
       public string ProveedorActual
       {
           get
           {
               var valor = _configuracion.GetValue<string>("DatabaseProvider");
               return string.IsNullOrWhiteSpace(valor) ? "SqlServer" : valor.Trim();
           }
       }
       public string ObtenerCadenaConexion()
       {
           string? cadena = _configuracion.GetConnectionString(ProveedorActual);
           if (string.IsNullOrWhiteSpace(cadena))
           {
               throw new InvalidOperationException(
                   $"No se encontró la cadena de conexión para el proveedor '{ProveedorActual}'. " +
                   $"Verificar que existe 'ConnectionStrings:{ProveedorActual}' en appsettings.json " +
                   $"y que 'DatabaseProvider' esté configurado correctamente. " +
                   $"Archivos a revisar: appsettings.json, appsettings.Development.json"
               );
           }
           return cadena;
       }
   }
}
