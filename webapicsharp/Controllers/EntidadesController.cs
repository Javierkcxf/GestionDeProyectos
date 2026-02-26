
using System;                                             // Para DateTime y excepciones básicas
using Microsoft.AspNetCore.Authorization;                // Para [AllowAnonymous] y políticas de autorización
using Microsoft.AspNetCore.Mvc;                          // Para ControllerBase, IActionResult, atributos HTTP
using System.Threading.Tasks;                            // Para async/await
using Microsoft.Extensions.Logging;                      // Para ILogger y logging estructurado
using Microsoft.Extensions.Configuration;                // Para IConfiguration y acceso a appsettings.json
using webapicsharp.Servicios.Abstracciones;              // Para IServicioCrud
using Microsoft.Data.SqlClient;                     // Para SqlException en manejo de errores específicos
using System.Text.Json;
namespace webapicsharp.Controllers
{
    [Route("api/{tabla}")]                                // Ruta dinámica: /api/usuarios, /api/productos, etc.
    [ApiController]                                       // Activa validación automática, binding, y comportamientos de API REST
    public class EntidadesController : ControllerBase
    {
        private readonly IServicioCrud _servicioCrud;           // Para lógica de negocio CRUD y reglas del dominio
        private readonly ILogger<EntidadesController> _logger;  // Para logging estructurado, auditoría y debugging
        private readonly IConfiguration _configuration;         // Para acceso a configuraciones desde appsettings.json
        public EntidadesController(
            IServicioCrud servicioCrud,           // Lógica de negocio y coordinación de operaciones CRUD
            ILogger<EntidadesController> logger,  // Logging estructurado, auditoría y monitoreo de operaciones
            IConfiguration configuration         // Acceso a configuraciones desde appsettings.json y otras fuentes
        )
        {
            _servicioCrud = servicioCrud ?? throw new ArgumentNullException(
                nameof(servicioCrud),
                "IServicioCrud no fue inyectado correctamente. Verificar registro de servicios en Program.cs"
            );
            _logger = logger ?? throw new ArgumentNullException(
                nameof(logger),
                "ILogger no fue inyectado correctamente. Problema en configuración de logging de ASP.NET Core"
            );
            _configuration = configuration ?? throw new ArgumentNullException(
                nameof(configuration),
                "IConfiguration no fue inyectado correctamente. Problema en configuración base de ASP.NET Core"
            );
        }
        [Authorize]
        [HttpGet]                                        // Responde exclusivamente a peticiones HTTP GET
        public async Task<IActionResult> ListarAsync(
            string tabla,                                 // Del path de la URL: /api/{tabla}
            [FromQuery] string? esquema,                  // Del query string: ?esquema=valor
            [FromQuery] int? limite                       // Del query string: ?limite=valor
        )
        {
            try
            {
                _logger.LogInformation(
                    "INICIO consulta - Tabla: {Tabla}, Esquema: {Esquema}, Límite: {Limite}",
                    tabla,                                // Tabla que se está consultando
                    esquema ?? "por defecto",            // Esquema especificado o indicador de valor por defecto
                    limite?.ToString() ?? "por defecto"  // Límite especificado o indicador de valor por defecto
                );
                var filas = await _servicioCrud.ListarAsync(tabla, esquema, limite);
                _logger.LogInformation(
                    "RESULTADO exitoso - Registros obtenidos: {Cantidad} de tabla {Tabla}",
                    filas.Count,    // Cantidad exacta de registros devueltos
                    tabla          // Tabla que fue consultada exitosamente
                );
                if (filas.Count == 0)
                {
                    _logger.LogInformation(
                        "SIN DATOS - Tabla {Tabla} consultada exitosamente pero no contiene registros",
                        tabla
                    );
                    return NoContent();
                }
                return Ok(new
                {
                    tabla = tabla,                              // Tabla que fue consultada (confirmación)
                    esquema = esquema ?? "por defecto",         // Esquema usado, con indicador legible para null
                    limite = limite,                            // Límite aplicado (puede ser null si no se especificó)
                    total = filas.Count,                        // Cantidad exacta de registros devueltos
                    datos = filas                               // Lista de registros obtenidos de la base de datos
                });
            }
            catch (ArgumentException excepcionArgumento)
            {
                _logger.LogWarning(
                    "ERROR DE VALIDACIÓN - Petición rechazada - Tabla: {Tabla}, Error: {Mensaje}",
                    tabla,                          // Tabla que se intentó consultar
                    excepcionArgumento.Message      // Mensaje específico de la validación que falló
                );
                return BadRequest(new
                {
                    estado = 400,                                    // Código de estado HTTP explícito
                    mensaje = "Parámetros de entrada inválidos.",    // Mensaje general para el usuario final
                    detalle = excepcionArgumento.Message,            // Detalle específico del problema de validación
                    tabla = tabla                                    // Contexto: tabla que se intentó consultar
                });
            }
            catch (InvalidOperationException excepcionOperacion)
            {
                _logger.LogError(excepcionOperacion,
                    "ERROR DE OPERACIÓN - Fallo en consulta - Tabla: {Tabla}, Error: {Mensaje}",
                    tabla,                              // Tabla que se intentó consultar
                    excepcionOperacion.Message          // Mensaje específico del error operacional
                );
                return NotFound(new
                {
                    estado = 404,                                      // Código de estado HTTP explícito
                    mensaje = "El recurso solicitado no fue encontrado.", // Mensaje general user-friendly
                    detalle = excepcionOperacion.Message,              // Detalle específico (ej: "tabla no existe")
                    tabla = tabla,                                     // Contexto: tabla que se buscó
                    sugerencia = "Verifique que la tabla y el esquema existan en la base de datos" // Guía para resolver
                });
            }
            catch (UnauthorizedAccessException excepcionAcceso)
            {
                _logger.LogWarning(
                    "ACCESO DENEGADO - Tabla restringida: {Tabla}, Error: {Mensaje}",
                    tabla,
                    excepcionAcceso.Message
                );
                return StatusCode(403, new
                {
                    estado = 403,
                    mensaje = "Acceso denegado.",
                    detalle = excepcionAcceso.Message,
                    tabla = tabla
                });
            }
            catch (Exception excepcionGeneral)
            {
                _logger.LogError(excepcionGeneral,
                    "ERROR CRÍTICO - Falla inesperada en consulta - Tabla: {Tabla}",
                    tabla              // Tabla donde ocurrió el error crítico
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
                    estado = 500,                                        // Código de estado HTTP explícito
                    mensaje = "Error interno del servidor al consultar tabla.",
                    tabla = tabla,                                       // Contexto de la operación
                    tipoError = excepcionGeneral.GetType().Name,        // Tipo de excepción
                    detalle = excepcionGeneral.Message,                 // Mensaje principal
                    detalleCompleto = detalleError.ToString(),          // Desglose completo
                    errorInterno = excepcionGeneral.InnerException?.Message,
                    timestamp = DateTime.UtcNow,                        // Timestamp para correlación
                    sugerencia = "Revise los logs del servidor para más detalles o contacte al administrador."
                });
            }
        }
        [HttpGet("{nombreClave}/{valor}")]
        public async Task<IActionResult> ObtenerPorClaveAsync(
        string tabla,           // Del path: /api/{tabla}
        string nombreClave,     // Del path: /{nombreClave}
        string valor,           // Del path: /{valor}
        [FromQuery] string? esquema = null  // Del query string: ?esquema=valor
        )
        {
            try
            {
                _logger.LogInformation(
                    "INICIO filtrado - Tabla: {Tabla}, Esquema: {Esquema}, Clave: {Clave}, Valor: {Valor}",
                    tabla, esquema ?? "por defecto", nombreClave, valor
                );
                var filas = await _servicioCrud.ObtenerPorClaveAsync(tabla, esquema, nombreClave, valor);
                _logger.LogInformation(
                    "RESULTADO filtrado - {Cantidad} registros encontrados para {Clave}={Valor} en {Tabla}",
                    filas.Count, nombreClave, valor, tabla
                );
                if (filas.Count == 0)
                {
                    return NotFound(new
                    {
                        estado = 404,
                        mensaje = "No se encontraron registros",
                        detalle = $"No se encontró ningún registro con {nombreClave} = {valor} en la tabla {tabla}",
                        tabla = tabla,
                        esquema = esquema ?? "por defecto",
                        filtro = $"{nombreClave} = {valor}"
                    });
                }
                return Ok(new
                {
                    tabla = tabla,
                    esquema = esquema ?? "por defecto",
                    filtro = $"{nombreClave} = {valor}",
                    total = filas.Count,
                    datos = filas
                });
            }
            catch (UnauthorizedAccessException excepcionAcceso)
            {
                return StatusCode(403, new
                {
                    estado = 403,
                    mensaje = "Acceso denegado.",
                    detalle = excepcionAcceso.Message,
                    tabla = tabla
                });
            }
            catch (ArgumentException excepcionArgumento)
            {
                return BadRequest(new
                {
                    estado = 400,
                    mensaje = "Parámetros inválidos.",
                    detalle = excepcionArgumento.Message,
                    tabla = tabla
                });
            }
            catch (InvalidOperationException excepcionOperacion)
            {
                return NotFound(new
                {
                    estado = 404,
                    mensaje = "Recurso no encontrado.",
                    detalle = excepcionOperacion.Message,
                    tabla = tabla
                });
            }
            catch (Exception excepcionGeneral)
            {
                _logger.LogError(excepcionGeneral,
                    "ERROR CRÍTICO - Falla en filtrado - Tabla: {Tabla}, Clave: {Clave}, Valor: {Valor}",
                    tabla, nombreClave, valor
                );
                var detalleError = new System.Text.StringBuilder();
                detalleError.AppendLine($"Tipo: {excepcionGeneral.GetType().Name}");
                detalleError.AppendLine($"Mensaje: {excepcionGeneral.Message}");
                if (excepcionGeneral.InnerException != null)
                    detalleError.AppendLine($"Error interno: {excepcionGeneral.InnerException.Message}");
                return StatusCode(500, new
                {
                    estado = 500,
                    mensaje = "Error interno del servidor al filtrar registros.",
                    tabla = tabla,
                    filtro = $"{nombreClave} = {valor}",
                    tipoError = excepcionGeneral.GetType().Name,
                    detalle = excepcionGeneral.Message,
                    detalleCompleto = detalleError.ToString(),
                    errorInterno = excepcionGeneral.InnerException?.Message,
                    timestamp = DateTime.UtcNow,
                    sugerencia = "Revise los logs para más detalles."
                });
            }
        }
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> CrearAsync(
            string tabla,                                           // Del path: /api/{tabla}
            [FromBody] Dictionary<string, object?> datosEntidad,   // Del body: JSON con datos
            [FromQuery] string? esquema = null,                    // Query param: ?esquema=valor
            [FromQuery] string? camposEncriptar = null             // Query param: ?camposEncriptar=password,pin
        )
        {
            try
            {
                _logger.LogInformation(
                    "INICIO creación - Tabla: {Tabla}, Esquema: {Esquema}, Campos a encriptar: {CamposEncriptar}",
                    tabla, esquema ?? "por defecto", camposEncriptar ?? "ninguno"
                );
                if (datosEntidad == null || !datosEntidad.Any())
                {
                    return BadRequest(new
                    {
                        estado = 400,
                        mensaje = "Los datos de la entidad no pueden estar vacíos.",
                        tabla = tabla
                    });
                }
                var datosConvertidos = new Dictionary<string, object?>();
                foreach (var kvp in datosEntidad)
                {
                    if (kvp.Value is JsonElement elemento)
                    {
                        datosConvertidos[kvp.Key] = ConvertirJsonElement(elemento);
                    }
                    else
                    {
                        datosConvertidos[kvp.Key] = kvp.Value;
                    }
                }
                bool creado = await _servicioCrud.CrearAsync(tabla, esquema, datosConvertidos, camposEncriptar);
                if (creado)
                {
                    _logger.LogInformation(
                        "ÉXITO creación - Registro creado en tabla {Tabla}",
                        tabla
                    );
                    return Ok(new
                    {
                        estado = 200,
                        mensaje = "Registro creado exitosamente.",
                        tabla = tabla,
                        esquema = esquema ?? "por defecto"
                    });
                }
                else
                {
                    return StatusCode(500, new
                    {
                        estado = 500,
                        mensaje = "No se pudo crear el registro.",
                        tabla = tabla
                    });
                }
            }
            catch (UnauthorizedAccessException excepcionAcceso)
            {
                return StatusCode(403, new
                {
                    estado = 403,
                    mensaje = "Acceso denegado.",
                    detalle = excepcionAcceso.Message,
                    tabla = tabla
                });
            }
            catch (ArgumentException excepcionArgumento)
            {
                return BadRequest(new
                {
                    estado = 400,
                    mensaje = "Datos inválidos.",
                    detalle = excepcionArgumento.Message,
                    tabla = tabla
                });
            }
            catch (InvalidOperationException excepcionOperacion)
            {
                return StatusCode(500, new
                {
                    estado = 500,
                    mensaje = "Error en la operación.",
                    detalle = excepcionOperacion.Message,
                    tabla = tabla
                });
            }
            catch (Exception excepcionGeneral)
            {
                _logger.LogError(excepcionGeneral,
                    "ERROR CRÍTICO - Falla en creación - Tabla: {Tabla}",
                    tabla
                );
                var detalleError = new System.Text.StringBuilder();
                detalleError.AppendLine($"Tipo: {excepcionGeneral.GetType().Name}");
                detalleError.AppendLine($"Mensaje: {excepcionGeneral.Message}");
                if (excepcionGeneral.InnerException != null)
                    detalleError.AppendLine($"Error interno: {excepcionGeneral.InnerException.Message}");
                return StatusCode(500, new
                {
                    estado = 500,
                    mensaje = "Error interno del servidor al crear registro.",
                    tabla = tabla,
                    tipoError = excepcionGeneral.GetType().Name,
                    detalle = excepcionGeneral.Message,
                    detalleCompleto = detalleError.ToString(),
                    errorInterno = excepcionGeneral.InnerException?.Message,
                    timestamp = DateTime.UtcNow,
                    sugerencia = "Revise los logs para más detalles."
                });
            }
        }
        [AllowAnonymous]
        [HttpPut("{nombreClave}/{valorClave}")]
        public async Task<IActionResult> ActualizarAsync(
            string tabla,                                           // Del path: /api/{tabla}
            string nombreClave,                                     // Del path: /{nombreClave}
            string valorClave,                                      // Del path: /{valorClave}
            [FromBody] Dictionary<string, object?> datosEntidad,   // Del body: JSON con nuevos datos
            [FromQuery] string? esquema = null,                    // Query param: ?esquema=valor
            [FromQuery] string? camposEncriptar = null             // Query param: ?camposEncriptar=password,pin
        )
        {
            try
            {
                _logger.LogInformation(
                    "INICIO actualización - Tabla: {Tabla}, Clave: {Clave}={Valor}, Esquema: {Esquema}, Campos a encriptar: {CamposEncriptar}",
                    tabla, nombreClave, valorClave, esquema ?? "por defecto", camposEncriptar ?? "ninguno"
                );
                if (datosEntidad == null || !datosEntidad.Any())
                {
                    return BadRequest(new
                    {
                        estado = 400,
                        mensaje = "Los datos de actualización no pueden estar vacíos.",
                        tabla = tabla,
                        filtro = $"{nombreClave} = {valorClave}"
                    });
                }
                var datosConvertidos = new Dictionary<string, object?>();
                foreach (var kvp in datosEntidad)
                {
                    if (kvp.Value is JsonElement elemento)
                    {
                        datosConvertidos[kvp.Key] = ConvertirJsonElement(elemento);
                    }
                    else
                    {
                        datosConvertidos[kvp.Key] = kvp.Value;
                    }
                }
                int filasAfectadas = await _servicioCrud.ActualizarAsync(
                    tabla, esquema, nombreClave, valorClave, datosConvertidos, camposEncriptar
                );
                if (filasAfectadas > 0)
                {
                    _logger.LogInformation(
                        "ÉXITO actualización - {FilasAfectadas} filas actualizadas en tabla {Tabla} WHERE {Clave}={Valor}",
                        filasAfectadas, tabla, nombreClave, valorClave
                    );
                    return Ok(new
                    {
                        estado = 200,
                        mensaje = "Registro actualizado exitosamente.",
                        tabla = tabla,
                        esquema = esquema ?? "por defecto",
                        filtro = $"{nombreClave} = {valorClave}",
                        filasAfectadas = filasAfectadas,
                        camposEncriptados = camposEncriptar ?? "ninguno"
                    });
                }
                else
                {
                    return NotFound(new
                    {
                        estado = 404,
                        mensaje = "No se encontró el registro a actualizar.",
                        detalle = $"No existe un registro con {nombreClave} = {valorClave} en la tabla {tabla}",
                        tabla = tabla,
                        filtro = $"{nombreClave} = {valorClave}"
                    });
                }
            }
            catch (UnauthorizedAccessException excepcionAcceso)
            {
                return StatusCode(403, new
                {
                    estado = 403,
                    mensaje = "Acceso denegado.",
                    detalle = excepcionAcceso.Message,
                    tabla = tabla
                });
            }
            catch (ArgumentException excepcionArgumento)
            {
                return BadRequest(new
                {
                    estado = 400,
                    mensaje = "Parámetros inválidos.",
                    detalle = excepcionArgumento.Message,
                    tabla = tabla,
                    filtro = $"{nombreClave} = {valorClave}"
                });
            }
            catch (InvalidOperationException excepcionOperacion)
            {
                return StatusCode(500, new
                {
                    estado = 500,
                    mensaje = "Error en la operación de actualización.",
                    detalle = excepcionOperacion.Message,
                    tabla = tabla,
                    filtro = $"{nombreClave} = {valorClave}"
                });
            }
            catch (Exception excepcionGeneral)
            {
                _logger.LogError(excepcionGeneral,
                    "ERROR CRÍTICO - Falla en actualización - Tabla: {Tabla}, Clave: {Clave}={Valor}",
                    tabla, nombreClave, valorClave
                );
                var detalleError = new System.Text.StringBuilder();
                detalleError.AppendLine($"Tipo: {excepcionGeneral.GetType().Name}");
                detalleError.AppendLine($"Mensaje: {excepcionGeneral.Message}");
                if (excepcionGeneral.InnerException != null)
                    detalleError.AppendLine($"Error interno: {excepcionGeneral.InnerException.Message}");
                return StatusCode(500, new
                {
                    estado = 500,
                    mensaje = "Error interno del servidor al actualizar registro.",
                    tabla = tabla,
                    filtro = $"{nombreClave} = {valorClave}",
                    tipoError = excepcionGeneral.GetType().Name,
                    detalle = excepcionGeneral.Message,
                    detalleCompleto = detalleError.ToString(),
                    errorInterno = excepcionGeneral.InnerException?.Message,
                    timestamp = DateTime.UtcNow,
                    sugerencia = "Revise los logs para más detalles."
                });
            }
        }
        [AllowAnonymous]
        [HttpDelete("{nombreClave}/{valorClave}")]
        public async Task<IActionResult> EliminarAsync(
            string tabla,                                          // Del path: /api/{tabla}
            string nombreClave,                                    // Del path: /{nombreClave}
            string valorClave,                                     // Del path: /{valorClave}
            [FromQuery] string? esquema = null                     // Query param: ?esquema=valor
        )
        {
            try
            {
                _logger.LogInformation(
                    "INICIO eliminación - Tabla: {Tabla}, Clave: {Clave}={Valor}, Esquema: {Esquema}",
                    tabla, nombreClave, valorClave, esquema ?? "por defecto"
                );
                int filasEliminadas = await _servicioCrud.EliminarAsync(
                    tabla, esquema, nombreClave, valorClave
                );
                if (filasEliminadas > 0)
                {
                    _logger.LogInformation(
                        "ÉXITO eliminación - {FilasEliminadas} filas eliminadas de tabla {Tabla} WHERE {Clave}={Valor}",
                        filasEliminadas, tabla, nombreClave, valorClave
                    );
                    return Ok(new
                    {
                        estado = 200,
                        mensaje = "Registro eliminado exitosamente.",
                        tabla = tabla,
                        esquema = esquema ?? "por defecto",
                        filtro = $"{nombreClave} = {valorClave}",
                        filasEliminadas = filasEliminadas
                    });
                }
                else
                {
                    return NotFound(new
                    {
                        estado = 404,
                        mensaje = "No se encontró el registro a eliminar.",
                        detalle = $"No existe un registro con {nombreClave} = {valorClave} en la tabla {tabla}",
                        tabla = tabla,
                        filtro = $"{nombreClave} = {valorClave}"
                    });
                }
            }
            catch (UnauthorizedAccessException excepcionAcceso)
            {
                return StatusCode(403, new
                {
                    estado = 403,
                    mensaje = "Acceso denegado.",
                    detalle = excepcionAcceso.Message,
                    tabla = tabla
                });
            }
            catch (ArgumentException excepcionArgumento)
            {
                return BadRequest(new
                {
                    estado = 400,
                    mensaje = "Parámetros inválidos.",
                    detalle = excepcionArgumento.Message,
                    tabla = tabla,
                    filtro = $"{nombreClave} = {valorClave}"
                });
            }
            catch (InvalidOperationException excepcionOperacion)
            {
                if (excepcionOperacion.InnerException is SqlException sqlEx &&
                    sqlEx.Number == 547)
                {
                    return Conflict(new
                    {
                        estado = 409,
                        mensaje = "No se puede eliminar el registro.",
                        detalle = "El registro está siendo referenciado por otros datos (restricción de clave foránea).",
                        tabla = tabla,
                        filtro = $"{nombreClave} = {valorClave}"
                    });
                }
                return StatusCode(500, new
                {
                    estado = 500,
                    mensaje = "Error en la operación de eliminación.",
                    detalle = excepcionOperacion.Message,
                    tabla = tabla,
                    filtro = $"{nombreClave} = {valorClave}"
                });
            }
            catch (Exception excepcionGeneral)
            {
                _logger.LogError(excepcionGeneral,
                    "ERROR CRÍTICO - Falla en eliminación - Tabla: {Tabla}, Clave: {Clave}={Valor}",
                    tabla, nombreClave, valorClave
                );
                var detalleError = new System.Text.StringBuilder();
                detalleError.AppendLine($"Tipo: {excepcionGeneral.GetType().Name}");
                detalleError.AppendLine($"Mensaje: {excepcionGeneral.Message}");
                if (excepcionGeneral.InnerException != null)
                    detalleError.AppendLine($"Error interno: {excepcionGeneral.InnerException.Message}");
                return StatusCode(500, new
                {
                    estado = 500,
                    mensaje = "Error interno del servidor al eliminar registro.",
                    tabla = tabla,
                    filtro = $"{nombreClave} = {valorClave}",
                    tipoError = excepcionGeneral.GetType().Name,
                    detalle = excepcionGeneral.Message,
                    detalleCompleto = detalleError.ToString(),
                    errorInterno = excepcionGeneral.InnerException?.Message,
                    timestamp = DateTime.UtcNow,
                    sugerencia = "Revise los logs para más detalles."
                });
            }
        }
        [AllowAnonymous]                                  // Acceso público para facilitar descubrimiento
        [HttpGet]                                         // Responde a peticiones GET
        [Route("api/info")]                               // Ruta específica que no interfiere con el patrón {tabla}
        public IActionResult ObtenerInformacion()
        {
            return Ok(new
            {
                controlador = "EntidadesController",
                version = "1.0",
                descripcion = "Controlador genérico para consultar tablas de base de datos",
                endpoints = new[]
                {
                   "GET /api/{tabla} - Lista registros de una tabla",
                   "GET /api/{tabla}?esquema={esquema} - Lista con esquema específico",
                   "GET /api/{tabla}?limite={numero} - Lista con límite de registros",
                   "GET /api/info - Muestra esta información"
               },
                ejemplos = new[]
                {
                   "GET /api/usuarios",
                   "GET /api/productos?esquema=ventas",
                   "GET /api/clientes?limite=50",
                   "GET /api/pedidos?esquema=ventas&limite=100"
               }
            });
        }
        [AllowAnonymous]                                  // Acceso público para bienvenida
        [HttpGet("/")]                                    // Mapea específicamente a la ruta raíz de la aplicación
        public IActionResult Inicio()
        {
            return Ok(new
            {
                Mensaje = "Bienvenido a la API Genérica en C#",
                Version = "1.0",
                Descripcion = "API genérica para operaciones CRUD sobre cualquier tabla de base de datos",
                Documentacion = "Para más detalles, visita /swagger",
                FechaServidor = DateTime.UtcNow,          // UTC para consistencia global y evitar problemas de zona horaria
                Enlaces = new
                {
                    Swagger = "/swagger",                 // Documentación interactiva completa
                    Info = "/api/info",                   // Información específica del controlador
                    EjemploTabla = "/api/MiTabla"        // Ejemplo de cómo usar el endpoint principal
                },
                Uso = new[]
                {
                   "GET /api/{tabla} - Lista registros de una tabla",
                   "GET /api/{tabla}?limite=50 - Lista con límite específico",
                   "GET /api/{tabla}?esquema=dbo - Lista con esquema específico"
               }
            });
        }
        private object? ConvertirJsonElement(JsonElement elemento)
        {
            return elemento.ValueKind switch
            {
                JsonValueKind.String => elemento.GetString(),
                JsonValueKind.Number => elemento.TryGetInt32(out int intValue)
                    ? intValue           // Número entero válido para int
                    : elemento.GetDouble(),  // Número decimal o muy grande
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Object => elemento.GetRawText(),
                JsonValueKind.Array => elemento.GetRawText(),
                _ => elemento.ToString()
            };
        }
        [AllowAnonymous]
        [HttpPost("verificar-contrasena")]
        public async Task<IActionResult> VerificarContrasenaAsync(
            string tabla,                                                    // Del path: /api/{tabla}
            [FromBody] Dictionary<string, object?> datos,                    // Del body: JSON con parámetros
            [FromQuery] string? esquema = null                               // Query param: ?esquema=valor
        )
        {
            try
            {
                _logger.LogInformation(
                    "INICIO verificación credenciales - Tabla: {Tabla}, Esquema: {Esquema}",
                    tabla, esquema ?? "por defecto"
                );
                if (datos == null || !datos.Any())
                {
                    return BadRequest(new
                    {
                        estado = 400,
                        mensaje = "Los parámetros de verificación no pueden estar vacíos.",
                        tabla = tabla
                    });
                }
                var datosConvertidos = new Dictionary<string, object?>();
                foreach (var kvp in datos)
                {
                    if (kvp.Value is JsonElement elemento)
                    {
                        datosConvertidos[kvp.Key] = ConvertirJsonElement(elemento);
                    }
                    else
                    {
                        datosConvertidos[kvp.Key] = kvp.Value;
                    }
                }
                var parametrosRequeridos = new[] { "campoUsuario", "campoContrasena", "valorUsuario", "valorContrasena" };
                foreach (var parametro in parametrosRequeridos)
                {
                    if (!datosConvertidos.ContainsKey(parametro) ||
                        string.IsNullOrWhiteSpace(datosConvertidos[parametro]?.ToString()))
                    {
                        return BadRequest(new
                        {
                            estado = 400,
                            mensaje = $"El parámetro '{parametro}' es requerido.",
                            tabla = tabla,
                            parametrosRequeridos = parametrosRequeridos
                        });
                    }
                }
                string campoUsuario = datosConvertidos["campoUsuario"]?.ToString() ?? "";
                string campoContrasena = datosConvertidos["campoContrasena"]?.ToString() ?? "";
                string valorUsuario = datosConvertidos["valorUsuario"]?.ToString() ?? "";
                string valorContrasena = datosConvertidos["valorContrasena"]?.ToString() ?? "";
                _logger.LogInformation(
                    "Verificando credenciales - Usuario: {Usuario}, Tabla: {Tabla}",
                    valorUsuario, tabla
                );
                var (codigo, mensaje) = await _servicioCrud.VerificarContrasenaAsync(
                    tabla, esquema, campoUsuario, campoContrasena, valorUsuario, valorContrasena
                );
                switch (codigo)
                {
                    case 200:
                        _logger.LogInformation(
                            "ÉXITO autenticación - Usuario {Usuario} autenticado correctamente en tabla {Tabla}",
                            valorUsuario, tabla
                        );
                        return Ok(new
                        {
                            estado = 200,
                            mensaje = "Credenciales verificadas exitosamente.",
                            tabla = tabla,
                            usuario = valorUsuario,
                            autenticado = true
                        });
                    case 404:
                        _logger.LogWarning(
                            "FALLO autenticación - Usuario {Usuario} no encontrado en tabla {Tabla}",
                            valorUsuario, tabla
                        );
                        return NotFound(new
                        {
                            estado = 404,
                            mensaje = "Usuario no encontrado.",
                            tabla = tabla,
                            usuario = valorUsuario,
                            autenticado = false
                        });
                    case 401:
                        _logger.LogWarning(
                            "FALLO autenticación - Contraseña incorrecta para usuario {Usuario} en tabla {Tabla}",
                            valorUsuario, tabla
                        );
                        return Unauthorized(new
                        {
                            estado = 401,
                            mensaje = "Contraseña incorrecta.",
                            tabla = tabla,
                            usuario = valorUsuario,
                            autenticado = false
                        });
                    default:
                        return StatusCode(500, new
                        {
                            estado = 500,
                            mensaje = "Error durante la verificación de credenciales.",
                            detalle = mensaje,
                            tabla = tabla
                        });
                }
            }
            catch (UnauthorizedAccessException excepcionAcceso)
            {
                return StatusCode(403, new
                {
                    estado = 403,
                    mensaje = "Acceso denegado.",
                    detalle = excepcionAcceso.Message,
                    tabla = tabla
                });
            }
            catch (ArgumentException excepcionArgumento)
            {
                return BadRequest(new
                {
                    estado = 400,
                    mensaje = "Parámetros inválidos.",
                    detalle = excepcionArgumento.Message,
                    tabla = tabla
                });
            }
            catch (Exception excepcionGeneral)
            {
                _logger.LogError(excepcionGeneral,
                    "ERROR CRÍTICO - Falla en verificación de credenciales - Tabla: {Tabla}",
                    tabla
                );
                var detalleError = new System.Text.StringBuilder();
                detalleError.AppendLine($"Tipo: {excepcionGeneral.GetType().Name}");
                detalleError.AppendLine($"Mensaje: {excepcionGeneral.Message}");
                if (excepcionGeneral.InnerException != null)
                    detalleError.AppendLine($"Error interno: {excepcionGeneral.InnerException.Message}");
                return StatusCode(500, new
                {
                    estado = 500,
                    mensaje = "Error interno del servidor al verificar credenciales.",
                    tabla = tabla,
                    tipoError = excepcionGeneral.GetType().Name,
                    detalle = excepcionGeneral.Message,
                    detalleCompleto = detalleError.ToString(),
                    errorInterno = excepcionGeneral.InnerException?.Message,
                    timestamp = DateTime.UtcNow,
                    sugerencia = "Revise los logs para más detalles."
                });
            }
        }
    }
}
