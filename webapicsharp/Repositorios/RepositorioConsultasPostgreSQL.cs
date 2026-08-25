
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using webapicsharp.Repositorios.Abstracciones;
using webapicsharp.Servicios.Abstracciones;
namespace webapicsharp.Repositorios
{
    public sealed class RepositorioConsultasPostgreSQL : IRepositorioConsultas
    {
        private readonly IProveedorConexion _proveedorConexion;
        public RepositorioConsultasPostgreSQL(IProveedorConexion proveedorConexion)
        {
            _proveedorConexion = proveedorConexion ?? throw new ArgumentNullException(nameof(proveedorConexion));
        }
        private NpgsqlDbType MapearTipo(string tipo)
        {
            return tipo.ToLower() switch
            {
                "text" => NpgsqlDbType.Text,
                "varchar" => NpgsqlDbType.Varchar,
                "character varying" => NpgsqlDbType.Varchar,
                "integer" => NpgsqlDbType.Integer,
                "int" => NpgsqlDbType.Integer,
                "int4" => NpgsqlDbType.Integer,
                "bigint" => NpgsqlDbType.Bigint,
                "int8" => NpgsqlDbType.Bigint,
                "smallint" => NpgsqlDbType.Smallint,
                "int2" => NpgsqlDbType.Smallint,
                "boolean" => NpgsqlDbType.Boolean,
                "bool" => NpgsqlDbType.Boolean,
                "json" => NpgsqlDbType.Json,
                "jsonb" => NpgsqlDbType.Jsonb,
                "timestamp" => NpgsqlDbType.Timestamp,
                "timestamp without time zone" => NpgsqlDbType.Timestamp,
                "timestamptz" => NpgsqlDbType.TimestampTz,
                "timestamp with time zone" => NpgsqlDbType.TimestampTz,
                "date" => NpgsqlDbType.Date,
                "numeric" => NpgsqlDbType.Numeric,
                "decimal" => NpgsqlDbType.Numeric,
                "real" => NpgsqlDbType.Real,
                "float4" => NpgsqlDbType.Real,
                "double precision" => NpgsqlDbType.Double,
                "float8" => NpgsqlDbType.Double,
                _ => NpgsqlDbType.Text
            };
        }
private async Task<List<(string Nombre, string Modo, string Tipo)>> ObtenerMetadatosParametrosAsync(
    NpgsqlConnection conexion,
    string nombreSP,
    string? esquema = null)
{
    var lista = new List<(string, string, string)>();
    string sql;
    if (!string.IsNullOrWhiteSpace(esquema))
    {
        sql = @"
            SELECT parameter_name, parameter_mode, data_type
            FROM information_schema.parameters
            WHERE specific_name = (
                SELECT specific_name
                FROM information_schema.routines
                WHERE routine_schema = @esquema
                  AND routine_name = @spName
                LIMIT 1
            )
            ORDER BY ordinal_position;";
    }
    else
    {
        sql = @"
            SELECT parameter_name, parameter_mode, data_type
            FROM information_schema.parameters
            WHERE specific_name = (
                SELECT specific_name
                FROM information_schema.routines
                WHERE routine_name = @spName
                ORDER BY CASE
                    WHEN routine_schema = 'public' THEN 1
                    ELSE 2
                END
                LIMIT 1
            )
            ORDER BY ordinal_position;";
    }
    await using var comando = new NpgsqlCommand(sql, conexion);
    comando.Parameters.AddWithValue("@spName", nombreSP);
    if (!string.IsNullOrWhiteSpace(esquema))
    {
        comando.Parameters.AddWithValue("@esquema", esquema);
    }
    await using var reader = await comando.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        string nombre = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
        string modo = reader.IsDBNull(1) ? "IN" : reader.GetString(1);
        string tipo = reader.IsDBNull(2) ? "text" : reader.GetString(2);
        lista.Add((nombre, modo, tipo));
    }
    return lista;
}
        public async Task<DataTable> EjecutarProcedimientoAlmacenadoConDictionaryAsync(
            string nombreSP,
            Dictionary<string, object?> parametros)
        {
            if (string.IsNullOrWhiteSpace(nombreSP))
                throw new ArgumentException("El nombre del procedimiento no puede estar vacío.");
            string cadenaConexion = _proveedorConexion.ObtenerCadenaConexion();
            await using var conexion = new NpgsqlConnection(cadenaConexion);
            await conexion.OpenAsync();
            string? esquema = null;
            string nombreSPSinEsquema = nombreSP;
            if (nombreSP.Contains('.'))
            {
                var partes = nombreSP.Split('.', 2);
                esquema = partes[0].Trim();
                nombreSPSinEsquema = partes[1].Trim();
            }
            nombreSPSinEsquema = nombreSPSinEsquema.Trim().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(esquema))
            {
                esquema = esquema.Trim().ToLowerInvariant();
            }
            string sqlTipo;
            if (!string.IsNullOrWhiteSpace(esquema))
            {
                sqlTipo = "SELECT routine_type FROM information_schema.routines WHERE routine_schema = @esquema AND routine_name = @spName LIMIT 1";
            }
            else
            {
                sqlTipo = @"SELECT routine_type FROM information_schema.routines
                           WHERE routine_name = @spName
                           ORDER BY CASE WHEN routine_schema = 'public' THEN 1 ELSE 2 END
                           LIMIT 1";
            }
            string tipoRutina = "PROCEDURE";
            await using (var cmdTipo = new NpgsqlCommand(sqlTipo, conexion))
            {
                cmdTipo.Parameters.AddWithValue("@spName", nombreSPSinEsquema);
                if (!string.IsNullOrWhiteSpace(esquema))
                {
                    cmdTipo.Parameters.AddWithValue("@esquema", esquema);
                }
                var resultado = await cmdTipo.ExecuteScalarAsync();
                tipoRutina = resultado?.ToString() ?? "PROCEDURE";
            }
            var metadatos = await ObtenerMetadatosParametrosAsync(conexion, nombreSPSinEsquema, esquema);
            var parametrosNormalizados = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in parametros ?? new Dictionary<string, object?>())
            {
                var clave = kv.Key.StartsWith("@") ? kv.Key.Substring(1) : kv.Key;
                parametrosNormalizados[clave] = kv.Value;
            }
            var parametrosEntrada = metadatos.Where(m => m.Modo == "IN").ToList();
            var placeholders = string.Join(", ", parametrosEntrada.Select((_, i) => $"${i + 1}"));
            string nombreCompletoSP = !string.IsNullOrWhiteSpace(esquema)
                ? $"{esquema}.{nombreSPSinEsquema}"
                : nombreSPSinEsquema;
            var sqlLlamada = tipoRutina == "FUNCTION"
                ? $"SELECT * FROM {nombreCompletoSP}({placeholders})"
                : $"CALL {nombreCompletoSP}({placeholders})";
            await using var comando = new NpgsqlCommand(sqlLlamada, conexion) { CommandTimeout = 300 };
            for (int i = 0; i < parametrosEntrada.Count; i++)
            {
                var meta = parametrosEntrada[i];
                string tipoMeta = meta.Tipo?.ToLower() ?? "text";
                object valor = parametrosNormalizados.TryGetValue(meta.Nombre, out var v) ? v ?? DBNull.Value : DBNull.Value;
                bool esJSON = tipoMeta is "json" or "jsonb";
                if (!esJSON && valor is string sValor && !string.IsNullOrWhiteSpace(sValor))
                {
                    var nombreParam = meta.Nombre?.ToLower() ?? "";
                    var sValorTrim = sValor.TrimStart();
                    if (sValorTrim.StartsWith("{") || sValorTrim.StartsWith("["))
                    {
                        esJSON = true;
                    }
                    else if (nombreParam.Contains("roles") || nombreParam.Contains("detalles") ||
                             nombreParam.Contains("json") || nombreParam.Contains("data"))
                    {
                        if (sValorTrim.StartsWith("{") || sValorTrim.StartsWith("["))
                        {
                            esJSON = true;
                        }
                    }
                }
                if (esJSON)
                {
                    var tipoJson = tipoMeta == "jsonb" ? NpgsqlDbType.Jsonb : NpgsqlDbType.Json;
                    object valorJson = valor == DBNull.Value ? DBNull.Value : (valor?.ToString() ?? "{}");
                    comando.Parameters.Add(new NpgsqlParameter { Value = valorJson, NpgsqlDbType = tipoJson });
                }
                else if (tipoMeta is "integer" or "int" or "int4")
                {
                    object valorInt = valor == DBNull.Value ? DBNull.Value : Convert.ToInt32(valor);
                    comando.Parameters.Add(new NpgsqlParameter { Value = valorInt, NpgsqlDbType = NpgsqlDbType.Integer });
                }
                else if (tipoMeta is "bigint" or "int8")
                {
                    object valorLong = valor == DBNull.Value ? DBNull.Value : Convert.ToInt64(valor);
                    comando.Parameters.Add(new NpgsqlParameter { Value = valorLong, NpgsqlDbType = NpgsqlDbType.Bigint });
                }
                else if (tipoMeta is "numeric" or "decimal")
                {
                    object valorDec = valor == DBNull.Value ? DBNull.Value : Convert.ToDecimal(valor);
                    comando.Parameters.Add(new NpgsqlParameter { Value = valorDec, NpgsqlDbType = NpgsqlDbType.Numeric });
                }
                else if (tipoMeta is "character varying" or "varchar" or "text")
                {
                    object valorStr = valor == DBNull.Value ? DBNull.Value : (valor?.ToString() ?? string.Empty);
                    comando.Parameters.Add(new NpgsqlParameter { Value = valorStr, NpgsqlDbType = MapearTipo(tipoMeta) });
                }
                else
                {
                    comando.Parameters.Add(new NpgsqlParameter { Value = valor, NpgsqlDbType = MapearTipo(tipoMeta) });
                }
            }
            var tabla = new DataTable();
            if (tipoRutina == "FUNCTION")
            {
                await using var reader = await comando.ExecuteReaderAsync();
                tabla.Load(reader);
            }
            else
            {
                await comando.ExecuteNonQueryAsync();
            }
            return tabla;
        }
        public async Task<DataTable> EjecutarConsultaParametrizadaConDictionaryAsync(
            string consultaSQL,
            Dictionary<string, object?> parametros,
            int maximoRegistros = 10000,
            string? esquema = null)
        {
            var tabla = new DataTable();
            string cadenaConexion = _proveedorConexion.ObtenerCadenaConexion();
            await using var conexion = new NpgsqlConnection(cadenaConexion);
            await conexion.OpenAsync();
            await using var comando = new NpgsqlCommand(consultaSQL, conexion);
            foreach (var p in parametros ?? new Dictionary<string, object?>())
            {
                string nombreParam = p.Key.StartsWith("@") ? p.Key : $"@{p.Key}";
                object? valor = p.Value ?? DBNull.Value;
                if (valor is DateTime dt && dt.TimeOfDay == TimeSpan.Zero)
                {
                    comando.Parameters.Add(new NpgsqlParameter(nombreParam, NpgsqlDbType.Date)
                    {
                        Value = DateOnly.FromDateTime(dt)
                    });
                }
                else
                {
                    comando.Parameters.AddWithValue(nombreParam, valor);
                }
            }
            await using var reader = await comando.ExecuteReaderAsync();
            tabla.Load(reader);
            return tabla;
        }
        public async Task<(bool esValida, string? mensajeError)> ValidarConsultaConDictionaryAsync(
            string consultaSQL,
            Dictionary<string, object?> parametros)
        {
            try
            {
                string cadenaConexion = _proveedorConexion.ObtenerCadenaConexion();
                await using var conexion = new NpgsqlConnection(cadenaConexion);
                await conexion.OpenAsync();
                string consultaValidacion = $"EXPLAIN {consultaSQL}";
                await using var comando = new NpgsqlCommand(consultaValidacion, conexion);
                foreach (var p in parametros ?? new Dictionary<string, object?>())
                {
                    string nombreParam = p.Key.StartsWith("@") ? p.Key : $"@{p.Key}";
                    comando.Parameters.AddWithValue(nombreParam, p.Value ?? DBNull.Value);
                }
                await comando.ExecuteNonQueryAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
        public async Task<string?> ObtenerEsquemaTablaAsync(string nombreTabla, string esquemaPredeterminado)
        {
            string cadenaConexion = _proveedorConexion.ObtenerCadenaConexion();
            await using var conexion = new NpgsqlConnection(cadenaConexion);
            await conexion.OpenAsync();
            await using var comando = new NpgsqlCommand(
                "SELECT table_schema FROM information_schema.tables WHERE table_name = @tabla LIMIT 1", conexion);
            comando.Parameters.AddWithValue("@tabla", nombreTabla);
            var resultado = await comando.ExecuteScalarAsync();
            return resultado?.ToString();
        }
        public async Task<DataTable> ObtenerEstructuraTablaAsync(string nombreTabla, string esquema)
        {
            var tabla = new DataTable();
            string cadenaConexion = _proveedorConexion.ObtenerCadenaConexion();
            await using var conexion = new NpgsqlConnection(cadenaConexion);
            await conexion.OpenAsync();
            await using var comando = new NpgsqlCommand(
                "SELECT column_name, data_type FROM information_schema.columns WHERE table_name = @tabla", conexion);
            comando.Parameters.AddWithValue("@tabla", nombreTabla);
            await using var reader = await comando.ExecuteReaderAsync();
            tabla.Load(reader);
            return tabla;
        }
        public async Task<DataTable> ObtenerEstructuraBaseDatosAsync(string? nombreBD)
        {
            var tabla = new DataTable();
            string cadenaConexion = _proveedorConexion.ObtenerCadenaConexion();
            await using var conexion = new NpgsqlConnection(cadenaConexion);
            await conexion.OpenAsync();
            await using var comando = new NpgsqlCommand(
                "SELECT table_name, column_name FROM information_schema.columns WHERE table_schema = 'public'", conexion);
            await using var reader = await comando.ExecuteReaderAsync();
            tabla.Load(reader);
            return tabla;
        }
    }
}
