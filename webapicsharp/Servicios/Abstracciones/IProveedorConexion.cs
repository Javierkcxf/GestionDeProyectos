
using System; // Para InvalidOperationException en documentación
namespace webapicsharp.Servicios.Abstracciones
{
    public interface IProveedorConexion
    {
        string ProveedorActual { get; }
        string ObtenerCadenaConexion();
    }
}
