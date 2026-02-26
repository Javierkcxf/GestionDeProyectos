
using System.Collections.Generic;   // Para usar List<> y Dictionary<> genéricos
using System.Threading.Tasks;       // Para programación asíncrona con async/await
namespace webapicsharp.Servicios.Abstracciones
{
    public interface IServicioCrud
    {
        Task<IReadOnlyList<Dictionary<string, object?>>> ListarAsync(
            string nombreTabla,    // Tabla a consultar (con validaciones de dominio)
            string? esquema,       // Esquema (con reglas de negocio aplicadas)
            int? limite           // Límite (con políticas empresariales aplicadas)
        );
        Task<IReadOnlyList<Dictionary<string, object?>>> ObtenerPorClaveAsync(
            string nombreTabla,
            string? esquema,
            string nombreClave,
            string valor
        );
        Task<bool> CrearAsync(
            string nombreTabla,
            string? esquema,
            Dictionary<string, object?> datos,
            string? camposEncriptar = null
        );
        Task<int> ActualizarAsync(
            string nombreTabla,
            string? esquema,
            string nombreClave,
            string valorClave,
            Dictionary<string, object?> datos,
            string? camposEncriptar = null
        );
        Task<int> EliminarAsync(
            string nombreTabla,
            string? esquema,
            string nombreClave,
            string valorClave
        );
        Task<(int codigo, string mensaje)> VerificarContrasenaAsync(
            string nombreTabla,
            string? esquema,
            string campoUsuario,
            string campoContrasena,
            string valorUsuario,
            string valorContrasena
        );
    }
}
