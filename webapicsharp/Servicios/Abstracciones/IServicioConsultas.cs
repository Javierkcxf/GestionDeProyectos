
using System.Collections.Generic;   // Para usar List<> y Dictionary<> genéricos
using System.Threading.Tasks;       // Para programación asíncrona con async/await
using Microsoft.Data.SqlClient;     // Para SqlParameter en las consultas parametrizadas
using System.Data;                  // Para DataTable
namespace webapicsharp.Servicios.Abstracciones
{
    public interface IServicioConsultas
    {
        (bool esValida, string? mensajeError) ValidarConsultaSQL(string consulta, string[] tablasProhibidas);
        Task<DataTable> EjecutarConsultaParametrizadaAsync(
            string consulta,
            List<SqlParameter> parametros,
            int maximoRegistros,
            string? esquema
        );
        Task<DataTable> EjecutarConsultaParametrizadaDesdeJsonAsync(
            string consulta,
            Dictionary<string, object?>? parametros
        );
        Task<DataTable> EjecutarProcedimientoAlmacenadoAsync(
            string nombreSP,
            Dictionary<string, object?>? parametros,
            List<string>? camposAEncriptar
        );
    }
}
