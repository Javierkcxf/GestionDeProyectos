
using System.Collections.Generic;   // Para Dictionary<>
using System.Threading.Tasks;       // Para async/await
using System.Data;                  // Para DataTable
namespace webapicsharp.Repositorios.Abstracciones
{
    public interface IRepositorioConsultas
    {
        Task<DataTable> EjecutarConsultaParametrizadaConDictionaryAsync(
            string consultaSQL,
            Dictionary<string, object?> parametros,
            int maximoRegistros = 10000,
            string? esquema = null
        );
        Task<(bool esValida, string? mensajeError)> ValidarConsultaConDictionaryAsync(
            string consultaSQL,
            Dictionary<string, object?> parametros
        );
        Task<DataTable> EjecutarProcedimientoAlmacenadoConDictionaryAsync(
            string nombreSP,
            Dictionary<string, object?> parametros
        );
        Task<string?> ObtenerEsquemaTablaAsync(string nombreTabla, string esquemaPredeterminado);
        Task<DataTable> ObtenerEstructuraTablaAsync(string nombreTabla, string esquema);
        Task<DataTable> ObtenerEstructuraBaseDatosAsync(string? nombreBD);
    }
}
