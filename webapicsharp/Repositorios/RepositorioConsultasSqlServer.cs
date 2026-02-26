
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using webapicsharp.Repositorios.Abstracciones;
using webapicsharp.Servicios.Abstracciones;
namespace webapicsharp.Repositorios
{
    public sealed class RepositorioConsultasSqlServer : IRepositorioConsultas
    {
        private readonly IProveedorConexion _proveedorConexion;
        public RepositorioConsultasSqlServer(IProveedorConexion proveedorConexion)
        {
            _proveedorConexion = proveedorConexion ?? throw new ArgumentNullException(nameof(proveedorConexion));
        }
        private SqlDbType MapearTipo(string tipo)
        {
            return tipo.ToLower() switch
            {
                "varchar" => SqlDbType.VarChar,
                "nvarchar" => SqlDbType.NVarChar,
                "char" => SqlDbType.Char,
                "nchar" => SqlDbType.NChar,
                "text" => SqlDbType.Text,
                "ntext" => SqlDbType.NText,
                "int" => SqlDbType.Int,
                "bigint" => SqlDbType.BigInt,
                "smallint" => SqlDbType.SmallInt,
                "tinyint" => SqlDbType.TinyInt,
                "bit" => SqlDbType.Bit,
                "decimal" => SqlDbType.Decimal,
                "numeric" => SqlDbType.Decimal,
                "money" => SqlDbType.Money,
                "smallmoney" => SqlDbType.SmallMoney,
                "float" => SqlDbType.Float,
                "real" => SqlDbType.Real,
                "datetime" => SqlDbType.DateTime,
                "datetime2" => SqlDbType.DateTime2,
                "smalldatetime" => SqlDbType.SmallDateTime,
                "date" => SqlDbType.Date,
                "time" => SqlDbType.Time,
                "datetimeoffset" => SqlDbType.DateTimeOffset,
                "uniqueidentifier" => SqlDbType.UniqueIdentifier,
                "binary" => SqlDbType.Binary,
                "varbinary" => SqlDbType.VarBinary,
                "image" => SqlDbType.Image,
                "xml" => SqlDbType.Xml,
                _ => SqlDbType.NVarChar
            };
        }
        private async Task<List<(string Nombre, bool EsOutput, string Tipo, int? MaxLength)>> ObtenerMetadatosParametrosAsync(
            SqlConnection conexion,
            string nombreSP)
        {
            var lista = new List<(string, bool, string, int?)>();
            string sql = @"
                SELECT
                    PARAMETER_NAME,
                    CASE WHEN PARAMETER_MODE = 'OUT' OR PARAMETER_MODE = 'INOUT' THEN 1 ELSE 0 END AS IsOutput,
                    DATA_TYPE,
                    CHARACTER_MAXIMUM_LENGTH
                FROM INFORMATION_SCHEMA.PARAMETERS
                WHERE SPECIFIC_NAME = @spName
                ORDER BY ORDINAL_POSITION;";
            await using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@spName", nombreSP);
            await using var reader = await comando.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                string nombre = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                bool esOutput = reader.IsDBNull(1) ? false : reader.GetInt32(1) == 1;
                string tipo = reader.IsDBNull(2) ? "nvarchar" : reader.GetString(2);
                int? maxLength = reader.IsDBNull(3) ? null : reader.GetInt32(3);
                lista.Add((nombre, esOutput, tipo, maxLength));
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
    await using var conexion = new SqlConnection(cadenaConexion);
    await conexion.OpenAsync();
    string sqlTipo = "SELECT ROUTINE_TYPE FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_NAME = @spName";
    string tipoRutina = "PROCEDURE";
    await using (var cmdTipo = new SqlCommand(sqlTipo, conexion))
    {
        cmdTipo.Parameters.AddWithValue("@spName", nombreSP);
        var resultado = await cmdTipo.ExecuteScalarAsync();
        tipoRutina = resultado?.ToString() ?? "PROCEDURE";
    }
    var metadatos = await ObtenerMetadatosParametrosAsync(conexion, nombreSP);
    var parametrosNormalizados = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    foreach (var kv in parametros ?? new Dictionary<string, object?>())
    {
        var clave = kv.Key.StartsWith("@") ? kv.Key.Substring(1) : kv.Key;
        parametrosNormalizados[clave] = kv.Value;
    }
    var tabla = new DataTable();
    if (tipoRutina == "FUNCTION")
    {
        var parametrosEntrada = metadatos.Where(m => !m.EsOutput).ToList();
        var parametrosQuery = string.Join(", ", parametrosEntrada.Select((_, i) => $"@p{i}"));
        var sqlLlamada = $"SELECT dbo.{nombreSP}({parametrosQuery}) AS Resultado";
        await using var comando = new SqlCommand(sqlLlamada, conexion);
        comando.CommandType = CommandType.Text;
        comando.CommandTimeout = 300;
        for (int i = 0; i < parametrosEntrada.Count; i++)
        {
            var meta = parametrosEntrada[i];
            string clave = meta.Nombre.StartsWith("@") ? meta.Nombre.Substring(1) : meta.Nombre;
            object valor = parametrosNormalizados.TryGetValue(clave, out var v) && v != null ? v : DBNull.Value;
            bool esJSON = meta.Tipo.ToLower() == "nvarchar" && meta.MaxLength == -1;
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
                var param = new SqlParameter($"@p{i}", SqlDbType.NVarChar) { Value = valor == DBNull.Value ? DBNull.Value : valor.ToString() };
                param.Size = -1;
                comando.Parameters.Add(param);
            }
            else if ((meta.Tipo.ToLower() == "varchar" || meta.Tipo.ToLower() == "nvarchar" || meta.Tipo.ToLower() == "char" || meta.Tipo.ToLower() == "nchar")
                     && valor != DBNull.Value)
            {
                string valorStr = valor.ToString() ?? string.Empty;
                var param = new SqlParameter($"@p{i}", MapearTipo(meta.Tipo)) { Value = valorStr };
                if (meta.MaxLength.HasValue && meta.MaxLength.Value > 0)
                    param.Size = meta.MaxLength.Value;
                comando.Parameters.Add(param);
            }
            else if (valor is DateTime dt && dt.TimeOfDay == TimeSpan.Zero && meta.Tipo.ToLower() == "date")
            {
                comando.Parameters.Add(new SqlParameter($"@p{i}", SqlDbType.Date) { Value = DateOnly.FromDateTime(dt) });
            }
            else if (meta.Tipo.ToLower() == "int" && valor != DBNull.Value)
            {
                comando.Parameters.Add(new SqlParameter($"@p{i}", SqlDbType.Int) { Value = Convert.ToInt32(valor) });
            }
            else if (meta.Tipo.ToLower() == "bigint" && valor != DBNull.Value)
            {
                comando.Parameters.Add(new SqlParameter($"@p{i}", SqlDbType.BigInt) { Value = Convert.ToInt64(valor) });
            }
            else if ((meta.Tipo.ToLower() == "decimal" || meta.Tipo.ToLower() == "numeric") && valor != DBNull.Value)
            {
                comando.Parameters.Add(new SqlParameter($"@p{i}", SqlDbType.Decimal) { Value = Convert.ToDecimal(valor) });
            }
            else if (meta.Tipo.ToLower() == "bit" && valor != DBNull.Value)
            {
                comando.Parameters.Add(new SqlParameter($"@p{i}", SqlDbType.Bit) { Value = Convert.ToBoolean(valor) });
            }
            else
            {
                var param = new SqlParameter($"@p{i}", MapearTipo(meta.Tipo)) { Value = valor };
                if (meta.MaxLength.HasValue && meta.MaxLength.Value > 0)
                    param.Size = meta.MaxLength.Value;
                comando.Parameters.Add(param);
            }
        }
        await using var reader = await comando.ExecuteReaderAsync();
        tabla.Load(reader);
    }
    else
    {
        await using var comando = new SqlCommand(nombreSP, conexion);
        comando.CommandType = CommandType.StoredProcedure;
        comando.CommandTimeout = 300;
        foreach (var meta in metadatos)
        {
            string clave = meta.Nombre!.StartsWith("@") ? meta.Nombre.Substring(1) : meta.Nombre;
            var sqlDbTipo = MapearTipo(meta.Tipo);
            if (!meta.EsOutput)
            {
                object valor = parametrosNormalizados.TryGetValue(clave, out var v) && v != null
                    ? v
                    : DBNull.Value;
                bool esJSON = meta.Tipo.ToLower() == "nvarchar" && meta.MaxLength == -1; // nvarchar(max)
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
                    var param = new SqlParameter($"@{clave}", SqlDbType.NVarChar)
                    {
                        Direction = ParameterDirection.Input,
                        Value = valor == DBNull.Value ? DBNull.Value : valor.ToString()
                    };
                    param.Size = -1; // -1 indica MAX
                    comando.Parameters.Add(param);
                }
                else if ((meta.Tipo.ToLower() == "varchar" || meta.Tipo.ToLower() == "nvarchar" || meta.Tipo.ToLower() == "char" || meta.Tipo.ToLower() == "nchar")
                         && valor != DBNull.Value)
                {
                    string valorStr = valor.ToString() ?? string.Empty;
                    var param = new SqlParameter($"@{clave}", MapearTipo(meta.Tipo))
                    {
                        Direction = ParameterDirection.Input,
                        Value = valorStr
                    };
                    if (meta.MaxLength.HasValue && meta.MaxLength.Value > 0)
                        param.Size = meta.MaxLength.Value;
                    comando.Parameters.Add(param);
                }
                else if (valor is DateTime dt && dt.TimeOfDay == TimeSpan.Zero && meta.Tipo.ToLower() == "date")
                {
                    var param = new SqlParameter($"@{clave}", SqlDbType.Date)
                    {
                        Direction = ParameterDirection.Input,
                        Value = DateOnly.FromDateTime(dt)
                    };
                    comando.Parameters.Add(param);
                }
                else if (meta.Tipo.ToLower() == "int" && valor != DBNull.Value)
                {
                    int valorInt = Convert.ToInt32(valor);
                    var param = new SqlParameter($"@{clave}", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Input,
                        Value = valorInt
                    };
                    comando.Parameters.Add(param);
                }
                else if (meta.Tipo.ToLower() == "bigint" && valor != DBNull.Value)
                {
                    long valorLong = Convert.ToInt64(valor);
                    var param = new SqlParameter($"@{clave}", SqlDbType.BigInt)
                    {
                        Direction = ParameterDirection.Input,
                        Value = valorLong
                    };
                    comando.Parameters.Add(param);
                }
                else if ((meta.Tipo.ToLower() == "decimal" || meta.Tipo.ToLower() == "numeric") && valor != DBNull.Value)
                {
                    decimal valorDec = Convert.ToDecimal(valor);
                    var param = new SqlParameter($"@{clave}", SqlDbType.Decimal)
                    {
                        Direction = ParameterDirection.Input,
                        Value = valorDec
                    };
                    comando.Parameters.Add(param);
                }
                else if (meta.Tipo.ToLower() == "bit" && valor != DBNull.Value)
                {
                    bool valorBool = Convert.ToBoolean(valor);
                    var param = new SqlParameter($"@{clave}", SqlDbType.Bit)
                    {
                        Direction = ParameterDirection.Input,
                        Value = valorBool
                    };
                    comando.Parameters.Add(param);
                }
                else
                {
                    var param = new SqlParameter($"@{clave}", sqlDbTipo)
                    {
                        Direction = ParameterDirection.Input,
                        Value = valor
                    };
                    if (meta.MaxLength.HasValue && meta.MaxLength.Value > 0)
                        param.Size = meta.MaxLength.Value;
                    comando.Parameters.Add(param);
                }
            }
            else
            {
                var param = new SqlParameter($"@{clave}", sqlDbTipo)
                {
                    Direction = ParameterDirection.Output
                };
                if (meta.MaxLength.HasValue && meta.MaxLength.Value > 0)
                    param.Size = meta.MaxLength.Value;
                comando.Parameters.Add(param);
            }
        }
        try
        {
            await using var reader = await comando.ExecuteReaderAsync();
            tabla.Load(reader);
        }
        catch
        {
            await comando.ExecuteNonQueryAsync();
        }
        foreach (SqlParameter param in comando.Parameters)
        {
            if (param.Direction == ParameterDirection.Output || param.Direction == ParameterDirection.InputOutput)
            {
                if (!tabla.Columns.Contains(param.ParameterName))
                    tabla.Columns.Add(param.ParameterName);
                if (tabla.Rows.Count == 0)
                    tabla.Rows.Add(tabla.NewRow());
                tabla.Rows[0][param.ParameterName] = param.Value == null ? DBNull.Value : param.Value;
            }
        }
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
            await using var conexion = new SqlConnection(cadenaConexion);
            await conexion.OpenAsync();
            await using var comando = new SqlCommand(consultaSQL, conexion);
            foreach (var p in parametros ?? new Dictionary<string, object?>())
            {
                string nombreParam = p.Key.StartsWith("@") ? p.Key : $"@{p.Key}";
                object? valor = p.Value ?? DBNull.Value;
                if (valor is DateTime dt && dt.TimeOfDay == TimeSpan.Zero)
                {
                    comando.Parameters.Add(new SqlParameter(nombreParam, SqlDbType.Date)
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
                await using var conexion = new SqlConnection(cadenaConexion);
                await conexion.OpenAsync();
                await using var comandoParseOn = new SqlCommand("SET PARSEONLY ON", conexion);
                await comandoParseOn.ExecuteNonQueryAsync();
                await using var comando = new SqlCommand(consultaSQL, conexion);
                comando.CommandTimeout = 5;
                foreach (var p in parametros ?? new Dictionary<string, object?>())
                {
                    string nombreParam = p.Key.StartsWith("@") ? p.Key : $"@{p.Key}";
                    comando.Parameters.AddWithValue(nombreParam, p.Value ?? DBNull.Value);
                }
                await comando.ExecuteNonQueryAsync();
                await using var comandoParseOff = new SqlCommand("SET PARSEONLY OFF", conexion);
                await comandoParseOff.ExecuteNonQueryAsync();
                return (true, null);
            }
            catch (SqlException sqlEx)
            {
                string mensajeError = sqlEx.Number switch
                {
                    102 => "Error de sintaxis SQL: revise la estructura de la consulta",
                    207 => "Nombre de columna inválido: verifique que las columnas existan",
                    208 => "Objeto no válido: tabla o vista no existe en la base de datos",
                    156 => "Palabra clave SQL incorrecta o en posición incorrecta",
                    170 => "Error de sintaxis cerca de palabra reservada",
                    _ => $"Error de validación SQL Server (Código {sqlEx.Number}): {sqlEx.Message}"
                };
                return (false, mensajeError);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
        public async Task<string?> ObtenerEsquemaTablaAsync(string nombreTabla, string esquemaPredeterminado)
        {
            string cadenaConexion = _proveedorConexion.ObtenerCadenaConexion();
            await using var conexion = new SqlConnection(cadenaConexion);
            await conexion.OpenAsync();
            string sql = @"
                SELECT TOP 1 TABLE_SCHEMA
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_NAME = @tabla
                ORDER BY
                    CASE WHEN TABLE_SCHEMA = @esquema THEN 0 ELSE 1 END,
                    TABLE_SCHEMA";
            await using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@tabla", nombreTabla);
            comando.Parameters.AddWithValue("@esquema", esquemaPredeterminado);
            var resultado = await comando.ExecuteScalarAsync();
            return resultado?.ToString();
        }
        public async Task<DataTable> ObtenerEstructuraTablaAsync(string nombreTabla, string esquema)
        {
            var tabla = new DataTable();
            string cadenaConexion = _proveedorConexion.ObtenerCadenaConexion();
            await using var conexion = new SqlConnection(cadenaConexion);
            await conexion.OpenAsync();
            string sql = @"
                SELECT
                    c.COLUMN_NAME AS Nombre,
                    c.DATA_TYPE AS TipoSql,
                    c.CHARACTER_MAXIMUM_LENGTH AS Longitud,
                    c.IS_NULLABLE AS Nullable,
                    c.COLUMN_DEFAULT AS ValorDefecto,
                    COLUMNPROPERTY(OBJECT_ID(QUOTENAME(c.TABLE_SCHEMA) + '.' + QUOTENAME(c.TABLE_NAME)), c.COLUMN_NAME, 'IsIdentity') AS EsIdentidad,
                    CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS EsPrimaria
                FROM INFORMATION_SCHEMA.COLUMNS c
                LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE pk
                    ON pk.TABLE_SCHEMA = c.TABLE_SCHEMA
                    AND pk.TABLE_NAME = c.TABLE_NAME
                    AND pk.COLUMN_NAME = c.COLUMN_NAME
                    AND OBJECTPROPERTY(OBJECT_ID(pk.CONSTRAINT_NAME), 'IsPrimaryKey') = 1
                WHERE c.TABLE_NAME = @tabla AND c.TABLE_SCHEMA = @esquema
                ORDER BY c.ORDINAL_POSITION";
            await using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@tabla", nombreTabla);
            comando.Parameters.AddWithValue("@esquema", esquema);
            await using var reader = await comando.ExecuteReaderAsync();
            tabla.Load(reader);
            return tabla;
        }
        public async Task<DataTable> ObtenerEstructuraBaseDatosAsync(string? nombreBD)
        {
            var tabla = new DataTable();
            string cadenaConexion = _proveedorConexion.ObtenerCadenaConexion();
            await using var conexion = new SqlConnection(cadenaConexion);
            await conexion.OpenAsync();
            string sql = @"
                SELECT
                    t.TABLE_SCHEMA AS Esquema,
                    t.TABLE_NAME AS Tabla,
                    c.COLUMN_NAME AS Columna,
                    c.DATA_TYPE AS TipoDato,
                    c.CHARACTER_MAXIMUM_LENGTH AS LongitudMaxima,
                    c.IS_NULLABLE AS Nullable,
                    CASE WHEN COLUMNPROPERTY(OBJECT_ID(t.TABLE_SCHEMA + '.' + t.TABLE_NAME), c.COLUMN_NAME, 'IsIdentity') = 1
                        THEN 'SI' ELSE 'NO' END AS Identidad,
                    c.ORDINAL_POSITION AS Posicion
                FROM INFORMATION_SCHEMA.TABLES t
                INNER JOIN INFORMATION_SCHEMA.COLUMNS c
                    ON t.TABLE_SCHEMA = c.TABLE_SCHEMA AND t.TABLE_NAME = c.TABLE_NAME
                WHERE t.TABLE_TYPE = 'BASE TABLE'
                ORDER BY t.TABLE_SCHEMA, t.TABLE_NAME, c.ORDINAL_POSITION";
            await using var comando = new SqlCommand(sql, conexion);
            await using var reader = await comando.ExecuteReaderAsync();
            tabla.Load(reader);
            return tabla;
        }
    }
}
