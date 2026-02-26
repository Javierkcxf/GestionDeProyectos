
using Microsoft.AspNetCore.Mvc;                      // Para el controlador y las acciones
using Microsoft.Extensions.Options;                  // Para inyectar configuraciones (IOptions)
using Microsoft.IdentityModel.Tokens;                // Para firmar y generar el token JWT
using System.IdentityModel.Tokens.Jwt;               // Para manipular JWT
using System.Security.Claims;                        // Para definir los claims dentro del token
using System.Text;                                   // Para codificar la clave secreta
using webapicsharp.Modelos;                          // Para la clase ConfiguracionJwt
using webapicsharp.Servicios.Abstracciones;           // Para la interfaz IServicioCrud
namespace webapicsharp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AutenticacionController : ControllerBase
    {
        private readonly ConfiguracionJwt _configuracionJwt;   // Configuración JWT cargada desde appsettings
        private readonly IServicioCrud _servicioCrud;
        private readonly ILogger<AutenticacionController> _logger;
        // Servicio genérico para verificar contraseñas
        public AutenticacionController(
            IOptions<ConfiguracionJwt> opcionesJwt,
            IServicioCrud servicioCrud,
            ILogger<AutenticacionController> logger)
        {
            _configuracionJwt = opcionesJwt.Value;
            _servicioCrud = servicioCrud;
            _logger = logger;
        }
        [HttpPost("token")]
        public async Task<IActionResult> GenerarToken([FromBody] CredencialesGenericas credenciales)
        {
            if (string.IsNullOrWhiteSpace(credenciales.Tabla) ||
                string.IsNullOrWhiteSpace(credenciales.CampoUsuario) ||
                string.IsNullOrWhiteSpace(credenciales.CampoContrasena) ||
                string.IsNullOrWhiteSpace(credenciales.Usuario) ||
                string.IsNullOrWhiteSpace(credenciales.Contrasena))
            {
                return BadRequest(new
                {
                    estado = 400,
                    mensaje = "Debe enviar tabla, campos y credenciales completas.",
                    ejemplo = new
                    {
                        tabla = "TablaDeUsuarios",
                        campoUsuario = "ejemploCampoUsuario",
                        campoContrasena = "ejemploCampoContrasena",
                        usuario = "ejemplo@correo.com",
                        contrasena = "admin123"
                    }
                });
            }
            var (codigo, mensaje) = await _servicioCrud.VerificarContrasenaAsync(
                credenciales.Tabla,
                null, // Esquema opcional
                credenciales.CampoUsuario,
                credenciales.CampoContrasena,
                credenciales.Usuario,
                credenciales.Contrasena
            );
            if (codigo == 404)
                return NotFound(new { estado = 404, mensaje = "Usuario no encontrado." });
            if (codigo == 401)
                return Unauthorized(new { estado = 401, mensaje = "Contraseña incorrecta." });
            if (codigo != 200)
                return StatusCode(500, new { estado = 500, mensaje = "Error interno durante la verificación.", detalle = mensaje });
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, credenciales.Usuario),       // Nombre de usuario
                new Claim("tabla", credenciales.Tabla),                 // Tabla usada para autenticación
                new Claim("campoUsuario", credenciales.CampoUsuario)    // Campo de usuario utilizado
            };
            var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuracionJwt.Key));
            var credencialesFirma = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256);
            var duracion = _configuracionJwt.DuracionMinutos > 0 ? _configuracionJwt.DuracionMinutos : 60;
            var token = new JwtSecurityToken(
                issuer: _configuracionJwt.Issuer,       // Emisor del token
                audience: _configuracionJwt.Audience,   // Público autorizado
                claims: claims,                         // Datos del usuario dentro del token
                expires: DateTime.UtcNow.AddMinutes(duracion), // Fecha de expiración
                signingCredentials: credencialesFirma   // Firma digital
            );
            string tokenGenerado = new JwtSecurityTokenHandler().WriteToken(token);
            return Ok(new
            {
                estado = 200,
                mensaje = "Autenticación exitosa.",
                usuario = credenciales.Usuario,
                token = tokenGenerado,
                expiracion = token.ValidTo
            });
        }

        [HttpGet("rutasrol/usuario/{emailUsuario}")]
        public async Task<IActionResult> ObtenerRutasRolesUsuarioAsync(string emailUsuario)
        {
            try
            {
                var resultado = new List<object>();

                var registrosRolUsuario = await _servicioCrud.ObtenerPorClaveAsync(
                    "rol_usuario", null, "fkemail", emailUsuario);

                if (registrosRolUsuario.Count == 0)
                    return Ok(resultado);

                foreach (var registro in registrosRolUsuario)
                {
                   
                    if (!registro.TryGetValue("fkidrol", out var idRolStr) ||
                        !int.TryParse(idRolStr?.ToString(), out var idRol))
                        continue;

                    // Obtener nombre del rol desde tabla rol
                    var registrosRol = await _servicioCrud.ObtenerPorClaveAsync("rol", null, "id", idRol.ToString());
                    if (registrosRol.Count == 0 || !registrosRol[0].TryGetValue("nombre", out var nombreRolObj))
                        continue;

                    var nombreRol = nombreRolObj?.ToString() ?? "";

                    // ❗ AQUÍ ESTABA EL PROBLEMA
                    // Antes buscabas por id → "fkidrol"
                    // Ahora buscamos por nombre → "rol"
                    var registrosRutas = await _servicioCrud.ObtenerPorClaveAsync("rutarol", null, "rol", nombreRol);

                    foreach (var rutaRegistro in registrosRutas)
                    {
                        if (rutaRegistro.TryGetValue("ruta", out var rutaObj))
                        {
                            resultado.Add(new
                            {
                                rutaUrl = rutaObj?.ToString(),
                                nombreRol = nombreRol
                            });
                        }
                    }
                }

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error en servidor: {ex.Message}");
            }
        }



        public class CredencialesGenericas
        {
            // Nombre de la tabla que contiene los usuarios (por ejemplo: "usuario", "vendedor", "cliente")
            public string Tabla { get; set; } = string.Empty;

            // Nombre del campo que almacena el identificador de usuario (por ejemplo: "email", "nombre", "login")
            public string CampoUsuario { get; set; } = string.Empty;

            // Nombre del campo que almacena la contraseña (por ejemplo: "clave", "password", "contrasena")
            public string CampoContrasena { get; set; } = string.Empty;

            // Valor del usuario que intenta autenticarse
            public string Usuario { get; set; } = string.Empty;

            // Contraseña enviada por el usuario (texto plano para comparar con hash en BD)
            public string Contrasena { get; set; } = string.Empty;
        }
    }
}
