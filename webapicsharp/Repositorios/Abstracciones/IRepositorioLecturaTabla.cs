
using System.Collections.Generic;   // Habilita List<> y Dictionary<> para colecciones
using System.Threading.Tasks;       // Habilita Task y async/await para operaciones asíncronas
namespace webapicsharp.Repositorios.Abstracciones
{
    public interface IRepositorioLecturaTabla
    {
        Task<IReadOnlyList<Dictionary<string, object?>>> ObtenerFilasAsync(
            string nombreTabla,
            string? esquema,
            int? limite
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
        Task<string?> ObtenerHashContrasenaAsync(
            string nombreTabla,
            string? esquema,
            string campoUsuario,
            string campoContrasena,
            string valorUsuario
        );
    }
}
