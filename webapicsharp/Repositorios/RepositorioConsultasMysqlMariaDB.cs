
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Data.SqlClient;                     // Recibe SqlParameter según la interfaz
using MySqlConnector;
using webapicsharp.Repositorios.Abstracciones;      // IRepositorioConsultas
using webapicsharp.Servicios.Abstracciones;         // IProveedorConexion
namespace webapicsharp.Repositorios
{
    public sealed class RepositorioConsultasMysqlMariaDB : IRepositorioConsultas
    {
        private readonly IProveedorConexion _proveedorConexion;
        public RepositorioConsultasMysqlMariaDB(IProveedorConexion proveedorConexion)
        {
            _proveedorConexion = proveedorConexion ?? throw new ArgumentNullException(nameof(proveedorConexion));
        }
        public async Task<DataTable> EjecutarConsultaParametrizadaAsync(
            string consultaSQL,
            List<SqlParameter>? parametros
        )
        {
            if (string.IsNullOrWhiteSpace(consultaSQL))
                throw new ArgumentException("La consulta SQL no puede estar vacía.", nameof(consultaSQL));
            var cadena = _proveedorConexion.ObtenerCadenaConexion();
            var tabla = new DataTable();
            await using var conexion = new MySqlConnection(cadena);
            await conexion.OpenAsync();
            await using var comando = new MySqlCommand(consultaSQL, conexion);
            AgregarParametros(comando, parametros);
            await using var lector = await comando.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
            tabla.Load(lector);
            return tabla;
        }
        public async Task<(bool esValida, string? mensajeError)> ValidarConsultaAsync(
            string consultaSQL,
            List<SqlParameter>? parametros
        )
        {
            if (string.IsNullOrWhiteSpace(consultaSQL))
                return (false, "La consulta SQL está vacía.");
            try
            {
                var cadena = _proveedorConexion.ObtenerCadenaConexion();
                await using var conexion = new MySqlConnection(cadena);
                await conexion.OpenAsync();
                await using var comando = new MySqlCommand(consultaSQL, conexion);
                AgregarParametros(comando, parametros);
                var esSelect = consultaSQL.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase);
                if (esSelect)
                {
                    await using var validar = new MySqlCommand($"EXPLAIN {consultaSQL}", conexion);
                    CopiarParametros(comando, validar);
                    await using var lectorValidar = await validar.ExecuteReaderAsync();
                }
                else
                {
                    _ = comando.CommandText;
                }
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
        public async Task<DataTable> EjecutarProcedimientoAlmacenadoAsync(
            string nombreSP,
            List<SqlParameter>? parametros
        )
        {
            if (string.IsNullOrWhiteSpace(nombreSP))
                throw new ArgumentException("El nombre del procedimiento no puede estar vacío.", nameof(nombreSP));
            var cadena = _proveedorConexion.ObtenerCadenaConexion();
            var tabla = new DataTable();
            var placeholders = ConstruirPlaceholders(parametros);
            await using var conexion = new MySqlConnection(cadena);
            await conexion.OpenAsync();
            string sqlCall = $"CALL {nombreSP}({placeholders})";
            await using (var cmdCall = new MySqlCommand(sqlCall, conexion))
            {
                AgregarParametros(cmdCall, parametros);
                try
                {
                    await using var lectorCall = await cmdCall.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
                    if (lectorCall.FieldCount > 0)
                    {
                        tabla.Load(lectorCall);
                        return tabla;
                    }
                }
                catch
                {
                }
            }
            string sqlSelect = $"SELECT * FROM {nombreSP}({placeholders})";
            await using (var cmdSel = new MySqlCommand(sqlSelect, conexion))
            {
                AgregarParametros(cmdSel, parametros);
                try
                {
                    await using var lectorSel = await cmdSel.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
                    tabla.Load(lectorSel);
                }
                catch
                {
                    string sqlScalar = $"SELECT {nombreSP}({placeholders}) AS resultado";
                    await using var cmdScalar = new MySqlCommand(sqlScalar, conexion);
                    AgregarParametros(cmdScalar, parametros);
                    await using var lectorScalar = await cmdScalar.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
                    tabla.Load(lectorScalar);
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
            var listaParametros = ConvertirDictionaryASqlParameter(parametros);
            return await EjecutarConsultaParametrizadaAsync(consultaSQL, listaParametros);
        }
        public async Task<(bool esValida, string? mensajeError)> ValidarConsultaConDictionaryAsync(
            string consultaSQL,
            Dictionary<string, object?> parametros)
        {
            var listaParametros = ConvertirDictionaryASqlParameter(parametros);
            return await ValidarConsultaAsync(consultaSQL, listaParametros);
        }
        public async Task<DataTable> EjecutarProcedimientoAlmacenadoConDictionaryAsync(
            string nombreSP,
            Dictionary<string, object?> parametros)
        {
            var listaParametros = ConvertirDictionaryASqlParameter(parametros);
            return await EjecutarProcedimientoAlmacenadoAsync(nombreSP, listaParametros);
        }
        public async Task<string?> ObtenerEsquemaTablaAsync(string nombreTabla, string esquemaPredeterminado)
        {
            if (string.IsNullOrWhiteSpace(nombreTabla))
                throw new ArgumentException("El nombre de la tabla no puede estar vacío.", nameof(nombreTabla));
            var cadena = _proveedorConexion.ObtenerCadenaConexion();
            await using var conexion = new MySqlConnection(cadena);
            await conexion.OpenAsync();
            const string sql1 = @"
                SELECT TABLE_SCHEMA
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_SCHEMA = @esquema AND TABLE_NAME = @tabla
                LIMIT 1;";
            await using (var cmd1 = new MySqlCommand(sql1, conexion))
            {
                cmd1.Parameters.AddWithValue("@esquema", string.IsNullOrWhiteSpace(esquemaPredeterminado) ? DatabaseActual(conexion) : esquemaPredeterminado);
                cmd1.Parameters.AddWithValue("@tabla", nombreTabla);
                var r1 = await cmd1.ExecuteScalarAsync();
                if (r1 != null && r1 is string s1) return s1;
            }
            const string sql2 = @"
                SELECT TABLE_SCHEMA
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_NAME = @tabla
                ORDER BY TABLE_SCHEMA
                LIMIT 1;";
            await using var cmd2 = new MySqlCommand(sql2, conexion);
            cmd2.Parameters.AddWithValue("@tabla", nombreTabla);
            var r2 = await cmd2.ExecuteScalarAsync();
            return r2 == null ? null : Convert.ToString(r2);
        }
        public async Task<DataTable> ObtenerEstructuraTablaAsync(string nombreTabla, string esquema)
        {
            if (string.IsNullOrWhiteSpace(nombreTabla))
                throw new ArgumentException("El nombre de la tabla no puede estar vacío.", nameof(nombreTabla));
            var cadena = _proveedorConexion.ObtenerCadenaConexion();
            var tabla = new DataTable();
            const string sql = @"
                SELECT
                    c.TABLE_SCHEMA   AS esquema,
                    c.TABLE_NAME     AS tabla,
                    c.COLUMN_NAME    AS columna,
                    c.ORDINAL_POSITION AS posicion,
                    c.DATA_TYPE      AS tipo_dato,
                    c.IS_NULLABLE    AS es_nulo,
                    c.CHARACTER_MAXIMUM_LENGTH AS longitud,
                    c.NUMERIC_PRECISION        AS precision,
                    c.NUMERIC_SCALE            AS escala,
                    c.COLUMN_DEFAULT           AS valor_por_defecto
                FROM INFORMATION_SCHEMA.COLUMNS c
                WHERE c.TABLE_SCHEMA = @esquema
                  AND c.TABLE_NAME   = @tabla
                ORDER BY c.ORDINAL_POSITION;";
            await using var conexion = new MySqlConnection(cadena);
            await conexion.OpenAsync();
            await using var cmd = new MySqlCommand(sql, conexion);
            cmd.Parameters.AddWithValue("@esquema", string.IsNullOrWhiteSpace(esquema) ? DatabaseActual(conexion) : esquema);
            cmd.Parameters.AddWithValue("@tabla", nombreTabla);
            await using var lector = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
            tabla.Load(lector);
            return tabla;
        }
        public async Task<DataTable> ObtenerEstructuraBaseDatosAsync(string? nombreBD)
        {
            var cadena = _proveedorConexion.ObtenerCadenaConexion();
            var tabla = new DataTable();
            const string sql = @"
                SELECT
                    c.TABLE_SCHEMA AS esquema,
                    c.TABLE_NAME   AS tabla,
                    c.COLUMN_NAME  AS columna,
                    c.DATA_TYPE    AS tipo_dato
                FROM INFORMATION_SCHEMA.COLUMNS c
                WHERE (@bd IS NULL OR c.TABLE_SCHEMA = @bd)
                ORDER BY c.TABLE_SCHEMA, c.TABLE_NAME, c.ORDINAL_POSITION;";
            await using var conexion = new MySqlConnection(cadena);
            await conexion.OpenAsync();
            await using var cmd = new MySqlCommand(sql, conexion);
            cmd.Parameters.AddWithValue("@bd", string.IsNullOrWhiteSpace(nombreBD) ? (object)DBNull.Value : nombreBD);
            await using var lector = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
            tabla.Load(lector);
            return tabla;
        }
        private static List<SqlParameter> ConvertirDictionaryASqlParameter(Dictionary<string, object?>? parametros)
        {
            var lista = new List<SqlParameter>();
            if (parametros == null || parametros.Count == 0) return lista;
            foreach (var kvp in parametros)
            {
                string nombre = kvp.Key.StartsWith("@") ? kvp.Key : $"@{kvp.Key}";
                object? valor = kvp.Value ?? DBNull.Value;
                lista.Add(new SqlParameter(nombre, valor));
            }
            return lista;
        }
        private static void AgregarParametros(MySqlCommand comando, List<SqlParameter>? parametros)
        {
            if (parametros == null || parametros.Count == 0) return;
            foreach (var p in parametros)
            {
                string nombre = p.ParameterName?.StartsWith("@") == true ? p.ParameterName : $"@{p.ParameterName}";
                var mp = new MySqlParameter(nombre, p.Value ?? DBNull.Value)
                {
                    Direction = p.Direction switch
                    {
                        ParameterDirection.Input => ParameterDirection.Input,
                        ParameterDirection.Output => ParameterDirection.Output,
                        ParameterDirection.InputOutput => ParameterDirection.InputOutput,
                        ParameterDirection.ReturnValue => ParameterDirection.ReturnValue,
                        _ => ParameterDirection.Input
                    }
                };
                if (p.Size > 0) mp.Size = p.Size;
                if (p.DbType != DbType.Object && p.DbType != 0)
                {
                    mp.DbType = p.DbType;
                }
                comando.Parameters.Add(mp);
            }
        }
        private static void CopiarParametros(MySqlCommand origen, MySqlCommand destino)
        {
            foreach (MySqlParameter p in origen.Parameters)
            {
                var copia = new MySqlParameter(p.ParameterName, p.Value)
                {
                    Direction = p.Direction,
                    Size = p.Size,
                    DbType = p.DbType
                };
                destino.Parameters.Add(copia);
            }
        }
        private static string ConstruirPlaceholders(List<SqlParameter>? parametros)
        {
            if (parametros == null || parametros.Count == 0) return string.Empty;
            var nombres = new List<string>(parametros.Count);
            foreach (var p in parametros)
            {
                string nombre = p.ParameterName?.StartsWith("@") == true ? p.ParameterName : $"@{p.ParameterName}";
                nombres.Add(nombre);
            }
            return string.Join(", ", nombres);
        }
        private static string DatabaseActual(MySqlConnection conexion)
        {
            return string.IsNullOrWhiteSpace(conexion.Database) ? "information_schema" : conexion.Database;
        }
    }
}
