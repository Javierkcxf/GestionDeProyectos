
using System;                                             // Para ArgumentException y ArgumentNullException
using System.Collections.Generic;                        // Para List<> y Dictionary<>
using System.Threading.Tasks;                            // Para async/await
using webapicsharp.Servicios.Abstracciones;             // Para IServicioCrud e IPoliticaTablasProhibidas
using webapicsharp.Repositorios.Abstracciones;          // Para IRepositorioLecturaTabla
namespace webapicsharp.Servicios
{
    public class ServicioCrud : IServicioCrud
    {
        private readonly IRepositorioLecturaTabla _repositorioLectura;
        private readonly IPoliticaTablasProhibidas _politicaTablasProhibidas;
        public ServicioCrud(
            IRepositorioLecturaTabla repositorioLectura,
            IPoliticaTablasProhibidas politicaTablasProhibidas)
        {
            _repositorioLectura = repositorioLectura ?? throw new ArgumentNullException(
                nameof(repositorioLectura),
                "IRepositorioLecturaTabla no puede ser null. " +
                "Verificar que esté registrado en Program.cs con AddScoped<IRepositorioLecturaTabla, ...>()"
            );
            _politicaTablasProhibidas = politicaTablasProhibidas ?? throw new ArgumentNullException(
                nameof(politicaTablasProhibidas),
                "IPoliticaTablasProhibidas no puede ser null. " +
                "Verificar que esté registrado en Program.cs con AddSingleton<IPoliticaTablasProhibidas, PoliticaTablasProhibidasDesdeJson>()"
            );
        }
        public async Task<IReadOnlyList<Dictionary<string, object?>>> ListarAsync(
            string nombreTabla,
            string? esquema,
            int? limite
        )
        {
            if (string.IsNullOrWhiteSpace(nombreTabla))
                throw new ArgumentException(
                    "El nombre de la tabla no puede estar vacío.",
                    nameof(nombreTabla)
                );
            if (!_politicaTablasProhibidas.EsTablaPermitida(nombreTabla))
            {
                throw new UnauthorizedAccessException(
                    $"Acceso denegado: La tabla '{nombreTabla}' está restringida y no puede ser consultada. " +
                    $"Verifique los permisos de acceso o contacte al administrador del sistema."
                );
            }
            string? esquemaNormalizado = string.IsNullOrWhiteSpace(esquema) ? null : esquema.Trim();
            int? limiteNormalizado = (limite is null || limite <= 0) ? null : limite;
            var filas = await _repositorioLectura.ObtenerFilasAsync(nombreTabla, esquemaNormalizado, limiteNormalizado);
            return filas;
        }
        public async Task<IReadOnlyList<Dictionary<string, object?>>> ObtenerPorClaveAsync(
            string nombreTabla,
            string? esquema,
            string nombreClave,
            string valor
        )
        {
            if (string.IsNullOrWhiteSpace(nombreTabla))
                throw new ArgumentException("El nombre de la tabla no puede estar vacío.", nameof(nombreTabla));
            if (string.IsNullOrWhiteSpace(nombreClave))
                throw new ArgumentException("El nombre de la clave no puede estar vacío.", nameof(nombreClave));
            if (string.IsNullOrWhiteSpace(valor))
                throw new ArgumentException("El valor no puede estar vacío.", nameof(valor));
            if (!_politicaTablasProhibidas.EsTablaPermitida(nombreTabla))
                throw new UnauthorizedAccessException(
                    $"Acceso denegado: La tabla '{nombreTabla}' está restringida y no puede ser consultada."
                );
            string? esquemaNormalizado = string.IsNullOrWhiteSpace(esquema) ? null : esquema.Trim();
            string nombreClaveNormalizado = nombreClave.Trim();
            string valorNormalizado = valor.Trim();
            var filas = await _repositorioLectura.ObtenerPorClaveAsync(
                nombreTabla,
                esquemaNormalizado,
                nombreClaveNormalizado,
                valorNormalizado
            );
            return filas;
        }
        public async Task<bool> CrearAsync(
            string nombreTabla,
            string? esquema,
            Dictionary<string, object?> datos,
            string? camposEncriptar = null
        )
        {
            if (string.IsNullOrWhiteSpace(nombreTabla))
                throw new ArgumentException("El nombre de la tabla no puede estar vacío.", nameof(nombreTabla));
            if (datos == null || !datos.Any())
                throw new ArgumentException("Los datos no pueden estar vacíos.", nameof(datos));
            if (!_politicaTablasProhibidas.EsTablaPermitida(nombreTabla))
                throw new UnauthorizedAccessException(
                    $"Acceso denegado: La tabla '{nombreTabla}' está restringida y no puede ser modificada."
                );
            string? esquemaNormalizado = string.IsNullOrWhiteSpace(esquema) ? null : esquema.Trim();
            string? camposEncriptarNormalizados = string.IsNullOrWhiteSpace(camposEncriptar) ? null : camposEncriptar.Trim();
            return await _repositorioLectura.CrearAsync(
                nombreTabla,
                esquemaNormalizado,
                datos,
                camposEncriptarNormalizados
            );
        }
        public async Task<int> ActualizarAsync(
            string nombreTabla,
            string? esquema,
            string nombreClave,
            string valorClave,
            Dictionary<string, object?> datos,
            string? camposEncriptar = null
        )
        {
            if (string.IsNullOrWhiteSpace(nombreTabla))
                throw new ArgumentException("El nombre de la tabla no puede estar vacío.", nameof(nombreTabla));
            if (string.IsNullOrWhiteSpace(nombreClave))
                throw new ArgumentException("El nombre de la clave no puede estar vacío.", nameof(nombreClave));
            if (string.IsNullOrWhiteSpace(valorClave))
                throw new ArgumentException("El valor de la clave no puede estar vacío.", nameof(valorClave));
            if (datos == null || !datos.Any())
                throw new ArgumentException("Los datos a actualizar no pueden estar vacíos.", nameof(datos));
            if (!_politicaTablasProhibidas.EsTablaPermitida(nombreTabla))
                throw new UnauthorizedAccessException(
                    $"Acceso denegado: La tabla '{nombreTabla}' está restringida y no puede ser modificada."
                );
            string? esquemaNormalizado = string.IsNullOrWhiteSpace(esquema) ? null : esquema.Trim();
            string nombreClaveNormalizado = nombreClave.Trim();
            string valorClaveNormalizado = valorClave.Trim();
            string? camposEncriptarNormalizados = string.IsNullOrWhiteSpace(camposEncriptar) ? null : camposEncriptar.Trim();
            return await _repositorioLectura.ActualizarAsync(
                nombreTabla,
                esquemaNormalizado,
                nombreClaveNormalizado,
                valorClaveNormalizado,
                datos,
                camposEncriptarNormalizados
            );
        }
        public async Task<int> EliminarAsync(
            string nombreTabla,
            string? esquema,
            string nombreClave,
            string valorClave
        )
        {
            if (string.IsNullOrWhiteSpace(nombreTabla))
                throw new ArgumentException("El nombre de la tabla no puede estar vacío.", nameof(nombreTabla));
            if (string.IsNullOrWhiteSpace(nombreClave))
                throw new ArgumentException("El nombre de la clave no puede estar vacío.", nameof(nombreClave));
            if (string.IsNullOrWhiteSpace(valorClave))
                throw new ArgumentException("El valor de la clave no puede estar vacío.", nameof(valorClave));
            if (!_politicaTablasProhibidas.EsTablaPermitida(nombreTabla))
                throw new UnauthorizedAccessException(
                    $"Acceso denegado: La tabla '{nombreTabla}' está restringida y no puede ser modificada."
                );
            string? esquemaNormalizado = string.IsNullOrWhiteSpace(esquema) ? null : esquema.Trim();
            string nombreClaveNormalizado = nombreClave.Trim();
            string valorClaveNormalizado = valorClave.Trim();
            return await _repositorioLectura.EliminarAsync(
                nombreTabla,
                esquemaNormalizado,
                nombreClaveNormalizado,
                valorClaveNormalizado
            );
        }
        public async Task<(int codigo, string mensaje)> VerificarContrasenaAsync(
            string nombreTabla,
            string? esquema,
            string campoUsuario,
            string campoContrasena,
            string valorUsuario,
            string valorContrasena
        )
        {
            if (string.IsNullOrWhiteSpace(nombreTabla))
                throw new ArgumentException("El nombre de la tabla no puede estar vacío.", nameof(nombreTabla));
            if (string.IsNullOrWhiteSpace(campoUsuario))
                throw new ArgumentException("El campo de usuario no puede estar vacío.", nameof(campoUsuario));
            if (string.IsNullOrWhiteSpace(campoContrasena))
                throw new ArgumentException("El campo de contraseña no puede estar vacío.", nameof(campoContrasena));
            if (string.IsNullOrWhiteSpace(valorUsuario))
                throw new ArgumentException("El valor de usuario no puede estar vacío.", nameof(valorUsuario));
            if (string.IsNullOrWhiteSpace(valorContrasena))
                throw new ArgumentException("La contraseña no puede estar vacía.", nameof(valorContrasena));
            if (!_politicaTablasProhibidas.EsTablaPermitida(nombreTabla))
                throw new UnauthorizedAccessException(
                    $"Acceso denegado: La tabla '{nombreTabla}' está restringida y no puede ser consultada."
                );
            string? esquemaNormalizado = string.IsNullOrWhiteSpace(esquema) ? null : esquema.Trim();
            string campoUsuarioNormalizado = campoUsuario.Trim();
            string campoContrasenaNormalizado = campoContrasena.Trim();
            string valorUsuarioNormalizado = valorUsuario.Trim();
            try
            {
                string? hashAlmacenado = await _repositorioLectura.ObtenerHashContrasenaAsync(
                    nombreTabla,
                    esquemaNormalizado,
                    campoUsuarioNormalizado,
                    campoContrasenaNormalizado,
                    valorUsuarioNormalizado
                );
                if (hashAlmacenado == null)
                {
                    return (404, "Usuario no encontrado");
                }
                bool contrasenaCorrecta = webapicsharp.Servicios.Utilidades.EncriptacionBCrypt.Verificar(
                    valorContrasena,  // Contraseña en texto plano proporcionada
                    hashAlmacenado    // Hash BCrypt almacenado en base de datos
                );
                if (contrasenaCorrecta)
                {
                    return (200, "Credenciales válidas");
                }
                else
                {
                    return (401, "Contraseña incorrecta");
                }
            }
            catch (Exception excepcion)
            {
                throw new InvalidOperationException(
                    $"Error durante la verificación de credenciales: {excepcion.Message}",
                    excepcion
                );
            }
        }
    }
}
