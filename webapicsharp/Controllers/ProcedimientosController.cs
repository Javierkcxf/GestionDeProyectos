
using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using webapicsharp.Servicios.Abstracciones;
namespace webapicsharp.Controllers
{
    [Route("api/procedimientos")]
    [ApiController]
    public class ProcedimientosController : ControllerBase
    {
        private readonly IServicioConsultas _servicioConsultas;
        private readonly ILogger<ProcedimientosController> _logger;
        public ProcedimientosController(
            IServicioConsultas servicioConsultas,
            ILogger<ProcedimientosController> logger)
        {
            _servicioConsultas = servicioConsultas ?? throw new ArgumentNullException(nameof(servicioConsultas));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        [Authorize]
        [HttpPost("ejecutarsp")]
        public async Task<IActionResult> EjecutarProcedimientoAlmacenadoAsync(
            [FromBody] Dictionary<string, object?> parametrosSP,
            [FromQuery] string? camposEncriptar = null)
        {
            try
            {
                if (parametrosSP == null || !parametrosSP.TryGetValue("nombreSP", out var nombreSPObj) || nombreSPObj == null)
                {
                    return BadRequest("El parámetro 'nombreSP' es requerido.");
                }
                string nombreSP = nombreSPObj.ToString()!;
                var camposAEncriptar = string.IsNullOrWhiteSpace(camposEncriptar)
                    ? new List<string>()
                    : camposEncriptar.Split(',').Select(c => c.Trim()).ToList();
                var parametrosLimpios = new Dictionary<string, object?>();
                foreach (var kvp in parametrosSP)
                {
                    if (!kvp.Key.Equals("nombreSP", StringComparison.OrdinalIgnoreCase))
                    {
                        parametrosLimpios[kvp.Key] = kvp.Value;
                    }
                }
                _logger.LogInformation(
                    "INICIO ejecución SP - Procedimiento: {NombreSP}, Parámetros: {CantidadParametros}",
                    nombreSP,
                    parametrosLimpios.Count
                );
                var resultado = await _servicioConsultas.EjecutarProcedimientoAlmacenadoAsync(
                    nombreSP,
                    parametrosLimpios,
                    camposAEncriptar);
                var lista = new List<Dictionary<string, object?>>();
                foreach (DataRow fila in resultado.Rows)
                {
                    var filaDiccionario = resultado.Columns.Cast<DataColumn>()
                        .ToDictionary(
                            col => col.ColumnName,
                            col => fila[col] == DBNull.Value ? null : fila[col]
                        );
                    lista.Add(filaDiccionario);
                }
                _logger.LogInformation(
                    "ÉXITO ejecución SP - Procedimiento: {NombreSP}, Registros: {Cantidad}",
                    nombreSP,
                    lista.Count
                );
                return Ok(new
                {
                    Procedimiento = nombreSP,
                    Resultados = lista,
                    Total = lista.Count,
                    Mensaje = "Procedimiento ejecutado correctamente"
                });
            }
            catch (ArgumentException excepcionArgumento)
            {
                _logger.LogWarning(
                    "PARÁMETROS INVÁLIDOS - SP: {Mensaje}",
                    excepcionArgumento.Message
                );
                return BadRequest(new
                {
                    estado = 400,
                    mensaje = "Parámetros de entrada inválidos.",
                    detalle = excepcionArgumento.Message
                });
            }
            catch (Exception excepcionGeneral)
            {
                _logger.LogError(excepcionGeneral,
                    "ERROR CRÍTICO - Falla inesperada ejecutando SP"
                );
                var detalleError = new System.Text.StringBuilder();
                detalleError.AppendLine($"Tipo de error: {excepcionGeneral.GetType().Name}");
                detalleError.AppendLine($"Mensaje: {excepcionGeneral.Message}");
                if (excepcionGeneral.InnerException != null)
                {
                    detalleError.AppendLine($"Error interno: {excepcionGeneral.InnerException.Message}");
                }
                if (!string.IsNullOrEmpty(excepcionGeneral.StackTrace))
                {
                    var stackLines = excepcionGeneral.StackTrace.Split('\n').Take(3);
                    detalleError.AppendLine("Stack trace:");
                    foreach (var line in stackLines)
                    {
                        detalleError.AppendLine($"  {line.Trim()}");
                    }
                }
                return StatusCode(500, new
                {
                    estado = 500,
                    mensaje = "Error interno del servidor al ejecutar procedimiento almacenado.",
                    tipoError = excepcionGeneral.GetType().Name,
                    detalle = excepcionGeneral.Message,
                    detalleCompleto = detalleError.ToString(),
                    errorInterno = excepcionGeneral.InnerException?.Message,
                    timestamp = DateTime.UtcNow,
                    sugerencia = "Revise los logs del servidor para más detalles o contacte al administrador."
                });
            }
        }
    }
}
