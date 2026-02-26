
using Microsoft.Extensions.Configuration;     // Para leer appsettings.json
using System;                                  // Para StringComparer
using System.Collections.Generic;              // Para HashSet
using System.Linq;                             // Para operaciones LINQ
using webapicsharp.Servicios.Abstracciones;   // Para IPoliticaTablasProhibidas
namespace webapicsharp.Servicios.Politicas
{
    public class PoliticaTablasProhibidasDesdeJson : IPoliticaTablasProhibidas
    {
        private readonly HashSet<string> _tablasProhibidas;
        public PoliticaTablasProhibidasDesdeJson(IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(
                    nameof(configuration),
                    "IConfiguration no puede ser null. Verificar registro de servicios en Program.cs."
                );
            var tablasProhibidasArray = configuration.GetSection("TablasProhibidas")
                .Get<string[]>() ?? Array.Empty<string>();
            _tablasProhibidas = new HashSet<string>(
                tablasProhibidasArray.Where(t => !string.IsNullOrWhiteSpace(t)),
                StringComparer.OrdinalIgnoreCase
            );
        }
        public bool EsTablaPermitida(string nombreTabla)
        {
            if (string.IsNullOrWhiteSpace(nombreTabla))
                return false;
            return !_tablasProhibidas.Contains(nombreTabla);
        }
        public IReadOnlyCollection<string> ObtenerTablasProhibidas()
        {
            return _tablasProhibidas;
        }
        public bool TieneRestricciones()
        {
            return _tablasProhibidas.Count > 0;
        }
    }
}
