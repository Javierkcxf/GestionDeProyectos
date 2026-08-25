
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using webapicsharp.Repositorios.Abstracciones;
using webapicsharp.Servicios.Abstracciones;
using webapicsharp.Servicios.Utilidades;
namespace webapicsharp.Repositorios
{
    public sealed class RepositorioLecturaPostgreSQL : IRepositorioLecturaTabla
    {
        private readonly IProveedorConexion _proveedorConexion;
        public RepositorioLecturaPostgreSQL(IProveedorConexion proveedorConexion)
        {
            _proveedorConexion = proveedorConexion ?? throw new ArgumentNullException(nameof(proveedorConexion));
        }
        private async Task<Dictionary<string, string>> ObtenerMapaColumnasAsync(string nombreTabla, string esquema)
        {
            var mapa = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string sql = @"
                SELECT column_name
                FROM information_schema.columns
                WHERE table_schema = @esquema
                AND table_name = @tabla";
            try
            {
                string cadena = _proveedorConexion.ObtenerCadenaConexion();
                await using var conexion = new NpgsqlConnection(cadena);
                await conexion.OpenAsync();
                await using var comando = new NpgsqlCommand(sql, conexion);
                comando.Parameters.AddWithValue("esquema", esquema);
                comando.Parameters.AddWithValue("tabla", nombreTabla);
                await using var lector = await comando.ExecuteReaderAsync();
                while (await lector.ReadAsync())
                {
                    string columna = lector.GetString(0);
                    mapa[columna] = columna;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Advertencia: No se pudo obtener el mapa de columnas de {esquema}.{nombreTabla}: {ex.Message}");
            }
            return mapa;
        }
        // El frontend serializa los nombres de propiedades en camelCase (ej. "idProyectoPadre"),
        // pero las columnas reales en Postgres quedaron en PascalCase al migrar desde SQL Server
        // (ej. "IdProyectoPadre"). Postgres es sensible a mayúsculas en identificadores entre
        // comillas, así que sin esta resolución el INSERT/UPDATE falla con "column does not exist".
        private string ResolverNombreColumna(Dictionary<string, string> mapaColumnas, string nombreSolicitado)
        {
            return mapaColumnas.TryGetValue(nombreSolicitado, out var real) ? real : nombreSolicitado;
        }
        private async Task<NpgsqlDbType?> DetectarTipoColumnaAsync(string nombreTabla, string esquema, string nombreColumna)
        {
            string sql = @"
                SELECT data_type, udt_name
                FROM information_schema.columns
                WHERE table_schema = @esquema
                AND table_name = @tabla
                AND column_name = @columna";
            try
            {
                string cadena = _proveedorConexion.ObtenerCadenaConexion();
                await using var conexion = new NpgsqlConnection(cadena);
                await conexion.OpenAsync();
                await using var comando = new NpgsqlCommand(sql, conexion);
                comando.Parameters.AddWithValue("esquema", esquema);
                comando.Parameters.AddWithValue("tabla", nombreTabla);
                comando.Parameters.AddWithValue("columna", nombreColumna);
                await using var lector = await comando.ExecuteReaderAsync();
                if (await lector.ReadAsync())
                {
                    string dataType = lector.GetString(0);
                    string udtName = lector.GetString(1);
                    return MapearTipoPostgreSQL(dataType, udtName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Advertencia: No se pudo detectar tipo de columna {nombreColumna} en {esquema}.{nombreTabla}: {ex.Message}");
            }
            return null;
        }
        private NpgsqlDbType? MapearTipoPostgreSQL(string dataType, string udtName)
        {
            return dataType.ToLower() switch
            {
                "integer" or "int4" => NpgsqlDbType.Integer,
                "bigint" or "int8" => NpgsqlDbType.Bigint,
                "smallint" or "int2" => NpgsqlDbType.Smallint,
                "numeric" or "decimal" => NpgsqlDbType.Numeric,
                "real" or "float4" => NpgsqlDbType.Real,
                "double precision" or "float8" => NpgsqlDbType.Double,
                "character varying" or "varchar" => NpgsqlDbType.Varchar,
                "character" or "char" => NpgsqlDbType.Char,
                "text" => NpgsqlDbType.Text,
                "boolean" or "bool" => NpgsqlDbType.Boolean,
                "uuid" => NpgsqlDbType.Uuid,
                "timestamp without time zone" => NpgsqlDbType.Timestamp,
                "timestamp with time zone" => NpgsqlDbType.TimestampTz,
                "date" => NpgsqlDbType.Date,
                "time" => NpgsqlDbType.Time,
                "json" => NpgsqlDbType.Json,
                "jsonb" => NpgsqlDbType.Jsonb,
                _ => null
            };
        }
        private object ConvertirValor(string valor, NpgsqlDbType? tipoDestino)
        {
            if (tipoDestino == null) return valor;
            try
            {
                return tipoDestino switch
                {
                    NpgsqlDbType.Integer => int.Parse(valor),
                    NpgsqlDbType.Bigint => long.Parse(valor),
                    NpgsqlDbType.Smallint => short.Parse(valor),
                    NpgsqlDbType.Numeric => decimal.Parse(valor),
                    NpgsqlDbType.Real => float.Parse(valor),
                    NpgsqlDbType.Double => double.Parse(valor),
                    NpgsqlDbType.Boolean => bool.Parse(valor),
                    NpgsqlDbType.Uuid => Guid.Parse(valor),
                    NpgsqlDbType.Timestamp => DateTime.Parse(valor),
                    NpgsqlDbType.TimestampTz => DateTime.Parse(valor),
                    NpgsqlDbType.Date => ExtraerSoloFecha(valor),
                    NpgsqlDbType.Time => TimeOnly.Parse(valor),
                    NpgsqlDbType.Varchar => valor,
                    NpgsqlDbType.Char => valor,
                    NpgsqlDbType.Text => valor,
                    NpgsqlDbType.Json => valor,
                    NpgsqlDbType.Jsonb => valor,
                    _ => valor
                };
            }
            catch
            {
                return valor;
            }
        }
        private DateOnly ExtraerSoloFecha(string valor)
        {
            if (DateTime.TryParse(valor, out DateTime fechaCompleta))
                return DateOnly.FromDateTime(fechaCompleta);
            if (DateOnly.TryParse(valor, out DateOnly soloFecha))
                return soloFecha;
            throw new FormatException(
                $"No se pudo convertir '{valor}' a fecha. " +
                $"Formatos válidos: '2025-09-25', '2025-09-25T00:00:00'");
        }
        private bool EsFechaSinHora(string valor)
        {
            return valor.Length == 10 &&
                   valor.Count(c => c == '-') == 2 &&
                   !valor.Contains("T") &&
                   !valor.Contains(":");
        }
        public async Task<IReadOnlyList<Dictionary<string, object?>>> ObtenerFilasAsync(
            string nombreTabla,
            string? esquema,
            int? limite
        )
        {
            if (string.IsNullOrWhiteSpace(nombreTabla))
                throw new ArgumentException("El nombre de la tabla no puede estar vacío.", nameof(nombreTabla));
            // Postgres distingue mayúsculas en identificadores entre comillas; el frontend
            // no es consistente con el casing que envía (ej. "tipoProyecto" vs "tipoproyecto"),
            // así que se normaliza a minúsculas para que siempre resuelva a la misma tabla.
            nombreTabla = nombreTabla.Trim().ToLowerInvariant();
            string esquemaFinal = string.IsNullOrWhiteSpace(esquema) ? "public" : esquema.Trim().ToLowerInvariant();
            int limiteFinal = limite ?? 1000;
            string sql = $"SELECT * FROM \"{esquemaFinal}\".\"{nombreTabla}\" LIMIT @limite";
            var filas = new List<Dictionary<string, object?>>();
            try
            {
                string cadena = _proveedorConexion.ObtenerCadenaConexion();
                await using var conexion = new NpgsqlConnection(cadena);
                await conexion.OpenAsync();
                await using var comando = new NpgsqlCommand(sql, conexion);
                comando.Parameters.AddWithValue("limite", limiteFinal);
                await using var lector = await comando.ExecuteReaderAsync();
                while (await lector.ReadAsync())
                {
                    var fila = new Dictionary<string, object?>();
                    for (int i = 0; i < lector.FieldCount; i++)
                    {
                        string nombreColumna = lector.GetName(i);
                        object? valor = lector.IsDBNull(i) ? null : lector.GetValue(i);
                        fila[nombreColumna] = valor;
                    }
                    filas.Add(fila);
                }
            }
            catch (NpgsqlException ex)
            {
                throw new InvalidOperationException(
                    $"Error PostgreSQL al consultar tabla '{esquemaFinal}.{nombreTabla}': {ex.Message}",
                    ex);
            }
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
            nombreTabla = nombreTabla.Trim().ToLowerInvariant();
            string esquemaFinal = string.IsNullOrWhiteSpace(esquema) ? "public" : esquema.Trim().ToLowerInvariant();
            var filas = new List<Dictionary<string, object?>>();
            try
            {
                var mapaColumnas = await ObtenerMapaColumnasAsync(nombreTabla, esquemaFinal);
                nombreClave = ResolverNombreColumna(mapaColumnas, nombreClave);
                var tipoColumna = await DetectarTipoColumnaAsync(nombreTabla, esquemaFinal, nombreClave);
                bool esBusquedaFechaSoloEnTimestamp =
                    tipoColumna == NpgsqlDbType.Timestamp &&
                    EsFechaSinHora(valor);
                string sql;
                object valorConvertido;
                NpgsqlDbType tipoParametro;
                if (esBusquedaFechaSoloEnTimestamp)
                {
                    sql = $"SELECT * FROM \"{esquemaFinal}\".\"{nombreTabla}\" " +
                          $"WHERE CAST(\"{nombreClave}\" AS DATE) = @valor";
                    valorConvertido = ExtraerSoloFecha(valor);
                    tipoParametro = NpgsqlDbType.Date;
                }
                else
                {
                    sql = $"SELECT * FROM \"{esquemaFinal}\".\"{nombreTabla}\" WHERE \"{nombreClave}\" = @valor";
                    valorConvertido = ConvertirValor(valor, tipoColumna);
                    tipoParametro = tipoColumna ?? NpgsqlDbType.Text;
                }
                string cadena = _proveedorConexion.ObtenerCadenaConexion();
                await using var conexion = new NpgsqlConnection(cadena);
                await conexion.OpenAsync();
                await using var comando = new NpgsqlCommand(sql, conexion);
                if (tipoColumna.HasValue || esBusquedaFechaSoloEnTimestamp)
                {
                    var parametro = new NpgsqlParameter("valor", tipoParametro) { Value = valorConvertido };
                    comando.Parameters.Add(parametro);
                }
                else
                {
                    comando.Parameters.AddWithValue("valor", valor);
                }
                await using var lector = await comando.ExecuteReaderAsync();
                while (await lector.ReadAsync())
                {
                    var fila = new Dictionary<string, object?>();
                    for (int i = 0; i < lector.FieldCount; i++)
                    {
                        string nombreColumna = lector.GetName(i);
                        object? valorColumna = lector.IsDBNull(i) ? null : lector.GetValue(i);
                        fila[nombreColumna] = valorColumna;
                    }
                    filas.Add(fila);
                }
            }
            catch (NpgsqlException ex)
            {
                throw new InvalidOperationException(
                    $"Error PostgreSQL al filtrar tabla '{esquemaFinal}.{nombreTabla}' por {nombreClave}='{valor}': {ex.Message}",
                    ex);
            }
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
            nombreTabla = nombreTabla.Trim().ToLowerInvariant();
            string esquemaFinal = string.IsNullOrWhiteSpace(esquema) ? "public" : esquema.Trim().ToLowerInvariant();
            var datosFinales = new Dictionary<string, object?>(datos);
            if (!string.IsNullOrWhiteSpace(camposEncriptar))
            {
                var camposAEncriptar = camposEncriptar.Split(',')
                    .Select(c => c.Trim())
                    .Where(c => !string.IsNullOrEmpty(c))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var campo in camposAEncriptar)
                {
                    if (datosFinales.ContainsKey(campo) && datosFinales[campo] != null)
                    {
                        string valorOriginal = datosFinales[campo]?.ToString() ?? "";
                        datosFinales[campo] = EncriptacionBCrypt.Encriptar(valorOriginal);
                    }
                }
            }
            var mapaColumnas = await ObtenerMapaColumnasAsync(nombreTabla, esquemaFinal);
            datosFinales = datosFinales.ToDictionary(
                kvp => ResolverNombreColumna(mapaColumnas, kvp.Key),
                kvp => kvp.Value);
            var columnas = string.Join(", ", datosFinales.Keys.Select(k => $"\"{k}\""));
            var parametros = string.Join(", ", datosFinales.Keys.Select(k => $"@{k}"));
            string sql = $"INSERT INTO \"{esquemaFinal}\".\"{nombreTabla}\" ({columnas}) VALUES ({parametros})";
            try
            {
                string cadena = _proveedorConexion.ObtenerCadenaConexion();
                await using var conexion = new NpgsqlConnection(cadena);
                await conexion.OpenAsync();
                await using var comando = new NpgsqlCommand(sql, conexion);
                foreach (var kvp in datosFinales)
                {
                    var tipoColumna = await DetectarTipoColumnaAsync(nombreTabla, esquemaFinal, kvp.Key);
                    if (kvp.Value == null)
                    {
                        comando.Parameters.AddWithValue(kvp.Key, DBNull.Value);
                    }
                    else if (tipoColumna.HasValue && kvp.Value is string valorString)
                    {
                        object valorConvertido = ConvertirValor(valorString, tipoColumna);
                        var parametro = new NpgsqlParameter(kvp.Key, tipoColumna.Value) { Value = valorConvertido };
                        comando.Parameters.Add(parametro);
                    }
                    else
                    {
                        comando.Parameters.AddWithValue(kvp.Key, kvp.Value);
                    }
                }
                int filasAfectadas = await comando.ExecuteNonQueryAsync();
                return filasAfectadas > 0;
            }
            catch (NpgsqlException ex)
            {
                throw new InvalidOperationException(
                    $"Error PostgreSQL al insertar en tabla '{esquemaFinal}.{nombreTabla}': {ex.Message}",
                    ex);
            }
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
            nombreTabla = nombreTabla.Trim().ToLowerInvariant();
            string esquemaFinal = string.IsNullOrWhiteSpace(esquema) ? "public" : esquema.Trim().ToLowerInvariant();
            var datosFinales = new Dictionary<string, object?>(datos);
            if (!string.IsNullOrWhiteSpace(camposEncriptar))
            {
                var camposAEncriptar = camposEncriptar.Split(',')
                    .Select(c => c.Trim())
                    .Where(c => !string.IsNullOrEmpty(c))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var campo in camposAEncriptar)
                {
                    if (datosFinales.ContainsKey(campo) && datosFinales[campo] != null)
                    {
                        string valorOriginal = datosFinales[campo]?.ToString() ?? "";
                        datosFinales[campo] = EncriptacionBCrypt.Encriptar(valorOriginal);
                    }
                }
            }
            try
            {
                var mapaColumnas = await ObtenerMapaColumnasAsync(nombreTabla, esquemaFinal);
                nombreClave = ResolverNombreColumna(mapaColumnas, nombreClave);
                datosFinales = datosFinales.ToDictionary(
                    kvp => ResolverNombreColumna(mapaColumnas, kvp.Key),
                    kvp => kvp.Value);
                var tipoColumna = await DetectarTipoColumnaAsync(nombreTabla, esquemaFinal, nombreClave);
                object valorClaveConvertido = ConvertirValor(valorClave, tipoColumna);
                var clausulaSet = string.Join(", ", datosFinales.Keys.Select(k => $"\"{k}\" = @{k}"));
                string sql = $"UPDATE \"{esquemaFinal}\".\"{nombreTabla}\" SET {clausulaSet} WHERE \"{nombreClave}\" = @valorClave";
                string cadena = _proveedorConexion.ObtenerCadenaConexion();
                await using var conexion = new NpgsqlConnection(cadena);
                await conexion.OpenAsync();
                await using var comando = new NpgsqlCommand(sql, conexion);
                foreach (var kvp in datosFinales)
                {
                    var tipoColumnaSet = await DetectarTipoColumnaAsync(nombreTabla, esquemaFinal, kvp.Key);
                    if (kvp.Value == null)
                    {
                        comando.Parameters.AddWithValue(kvp.Key, DBNull.Value);
                    }
                    else if (tipoColumnaSet.HasValue && kvp.Value is string valorString)
                    {
                        object valorConvertido = ConvertirValor(valorString, tipoColumnaSet);
                        var parametro = new NpgsqlParameter(kvp.Key, tipoColumnaSet.Value) { Value = valorConvertido };
                        comando.Parameters.Add(parametro);
                    }
                    else
                    {
                        comando.Parameters.AddWithValue(kvp.Key, kvp.Value);
                    }
                }
                if (tipoColumna.HasValue)
                {
                    var parametro = new NpgsqlParameter("valorClave", tipoColumna.Value) { Value = valorClaveConvertido };
                    comando.Parameters.Add(parametro);
                }
                else
                {
                    comando.Parameters.AddWithValue("valorClave", valorClave);
                }
                int filasAfectadas = await comando.ExecuteNonQueryAsync();
                return filasAfectadas;
            }
            catch (NpgsqlException ex)
            {
                throw new InvalidOperationException(
                    $"Error PostgreSQL al actualizar tabla '{esquemaFinal}.{nombreTabla}' WHERE {nombreClave}='{valorClave}': {ex.Message}",
                    ex);
            }
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
            nombreTabla = nombreTabla.Trim().ToLowerInvariant();
            string esquemaFinal = string.IsNullOrWhiteSpace(esquema) ? "public" : esquema.Trim().ToLowerInvariant();
            try
            {
                var mapaColumnas = await ObtenerMapaColumnasAsync(nombreTabla, esquemaFinal);
                nombreClave = ResolverNombreColumna(mapaColumnas, nombreClave);
                var tipoColumna = await DetectarTipoColumnaAsync(nombreTabla, esquemaFinal, nombreClave);
                object valorConvertido = ConvertirValor(valorClave, tipoColumna);
                string sql = $"DELETE FROM \"{esquemaFinal}\".\"{nombreTabla}\" WHERE \"{nombreClave}\" = @valorClave";
                string cadena = _proveedorConexion.ObtenerCadenaConexion();
                await using var conexion = new NpgsqlConnection(cadena);
                await conexion.OpenAsync();
                await using var comando = new NpgsqlCommand(sql, conexion);
                if (tipoColumna.HasValue)
                {
                    var parametro = new NpgsqlParameter("valorClave", tipoColumna.Value) { Value = valorConvertido };
                    comando.Parameters.Add(parametro);
                }
                else
                {
                    comando.Parameters.AddWithValue("valorClave", valorClave);
                }
                int filasEliminadas = await comando.ExecuteNonQueryAsync();
                return filasEliminadas;
            }
            catch (NpgsqlException ex)
            {
                throw new InvalidOperationException(
                    $"Error PostgreSQL al eliminar de tabla '{esquemaFinal}.{nombreTabla}' WHERE {nombreClave}='{valorClave}': {ex.Message}",
                    ex);
            }
        }
        public async Task<string?> ObtenerHashContrasenaAsync(
            string nombreTabla,
            string? esquema,
            string campoUsuario,
            string campoContrasena,
            string valorUsuario
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
            nombreTabla = nombreTabla.Trim().ToLowerInvariant();
            string esquemaFinal = string.IsNullOrWhiteSpace(esquema) ? "public" : esquema.Trim().ToLowerInvariant();
            try
            {
                var mapaColumnas = await ObtenerMapaColumnasAsync(nombreTabla, esquemaFinal);
                campoUsuario = ResolverNombreColumna(mapaColumnas, campoUsuario);
                campoContrasena = ResolverNombreColumna(mapaColumnas, campoContrasena);
                var tipoColumna = await DetectarTipoColumnaAsync(nombreTabla, esquemaFinal, campoUsuario);
                object valorConvertido = ConvertirValor(valorUsuario, tipoColumna);
                string sql = $"SELECT \"{campoContrasena}\" FROM \"{esquemaFinal}\".\"{nombreTabla}\" WHERE \"{campoUsuario}\" = @valorUsuario";
                string cadena = _proveedorConexion.ObtenerCadenaConexion();
                await using var conexion = new NpgsqlConnection(cadena);
                await conexion.OpenAsync();
                await using var comando = new NpgsqlCommand(sql, conexion);
                if (tipoColumna.HasValue)
                {
                    var parametro = new NpgsqlParameter("valorUsuario", tipoColumna.Value) { Value = valorConvertido };
                    comando.Parameters.Add(parametro);
                }
                else
                {
                    comando.Parameters.AddWithValue("valorUsuario", valorUsuario);
                }
                var resultado = await comando.ExecuteScalarAsync();
                return resultado?.ToString();
            }
            catch (NpgsqlException ex)
            {
                throw new InvalidOperationException(
                    $"Error PostgreSQL al obtener hash de contraseña de tabla '{esquemaFinal}.{nombreTabla}': {ex.Message}",
                    ex);
            }
        }
    }
}
