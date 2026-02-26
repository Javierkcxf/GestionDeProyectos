using System.Net.Http.Json;
using Microsoft.JSInterop;
using System.Text.Json;
using System.Net;

namespace FrontendBlazorApi.Servicios
{
    public class ServicioApiGenerico
    {
        private readonly IHttpClientFactory _fabricaHttp;
        private readonly IJSRuntime _js;
        private const string NombreCliente = "ApiGenerica";

        public ServicioApiGenerico(IHttpClientFactory fabricaHttp, IJSRuntime js)
        {
            _fabricaHttp = fabricaHttp;
            _js = js;
        }

        private async Task<HttpClient> CrearClienteConTokenAsync()
        {
            var cliente = _fabricaHttp.CreateClient(NombreCliente);
            var token = await _js.InvokeAsync<string>("sessionStorage.getItem", "token");
            if (!string.IsNullOrWhiteSpace(token))
            {
                cliente.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            return cliente;
        }

        private async Task LanzarSiError(HttpResponseMessage respuesta)
        {
            if (respuesta.IsSuccessStatusCode)
                return;

            string detalle = "";

            try
            {
                var error = await respuesta.Content.ReadFromJsonAsync<ApiError>();
                detalle = error?.Mensaje ?? "";
            }
            catch
            {
                detalle = await respuesta.Content.ReadAsStringAsync();
            }

            string mensaje = respuesta.StatusCode switch
            {
                HttpStatusCode.BadRequest => $"Solicitud incorrecta (400). {detalle}",
                HttpStatusCode.Unauthorized => "Acceso no autorizado. Verifique sus credenciales o el token.",
                HttpStatusCode.Forbidden => "Acceso denegado. No tiene permisos suficientes.",
                HttpStatusCode.NotFound => "Recurso no encontrado en el servidor.",
                HttpStatusCode.InternalServerError => "Error interno en el servidor.",
                _ => $"Error inesperado ({(int)respuesta.StatusCode}). {detalle}"
            };

            throw new Exception(mensaje);
        }

        public async Task<T?> GetAsync<T>(string endpoint)
        {
            var cliente = await CrearClienteConTokenAsync();
            var respuesta = await cliente.GetAsync(endpoint);
            await LanzarSiError(respuesta);
            return await respuesta.Content.ReadFromJsonAsync<T>();
        }

        public async Task<T?> PostAsync<T>(string endpoint, object datos)
        {
            var cliente = await CrearClienteConTokenAsync();
            var respuesta = await cliente.PostAsJsonAsync(endpoint, datos);
            await LanzarSiError(respuesta);
            return await respuesta.Content.ReadFromJsonAsync<T>();
        }

        public async Task<T?> PostSinAutenticacionAsync<T>(string endpoint, object datos)
        {
            var cliente = _fabricaHttp.CreateClient(NombreCliente);
            var respuesta = await cliente.PostAsJsonAsync(endpoint, datos);
            await LanzarSiError(respuesta);
            return await respuesta.Content.ReadFromJsonAsync<T>();
        }

        public async Task<List<T>> ObtenerTodosAsync<T>(string tabla)
        {
            var cliente = await CrearClienteConTokenAsync();
            var respuesta = await cliente.GetAsync($"api/{tabla}");
            await LanzarSiError(respuesta);
            var resultado = await respuesta.Content.ReadFromJsonAsync<ApiRespuesta<List<T>>>();
            return resultado?.Datos ?? new List<T>();
        }

        public async Task<T?> ObtenerPorClaveAsync<T>(string tabla, string campoClave, object valor)
        {
            var cliente = await CrearClienteConTokenAsync();
            var respuesta = await cliente.GetAsync($"api/{tabla}/{campoClave}/{valor}");
            await LanzarSiError(respuesta);
            var resultado = await respuesta.Content.ReadFromJsonAsync<ApiRespuesta<T>>();
            return resultado == null ? default : resultado.Datos;
        }

        public async Task<string> CrearAsync<T>(string tabla, T entidad)
        {
            var cliente = await CrearClienteConTokenAsync();
            var respuesta = await cliente.PostAsJsonAsync($"api/{tabla}", entidad);
            await LanzarSiError(respuesta);
            return "Registro creado correctamente.";
        }

        public async Task<string> CrearAsync<T>(string tabla, T entidad, string camposEncriptar)
        {
            var cliente = await CrearClienteConTokenAsync();
            var url = $"api/{tabla}?camposEncriptar={camposEncriptar}";
            var respuesta = await cliente.PostAsJsonAsync(url, entidad);
            await LanzarSiError(respuesta);
            return "Registro creado correctamente.";
        }

        public async Task<string> ActualizarAsync<T>(string tabla, string campoClave, object valor, T entidad)
        {
            var cliente = await CrearClienteConTokenAsync();
            var respuesta = await cliente.PutAsJsonAsync($"api/{tabla}/{campoClave}/{valor}", entidad);
            await LanzarSiError(respuesta);
            return "Registro actualizado correctamente.";
        }

        public async Task<string> ActualizarAsync<T>(string tabla, string campoClave, object valor, T entidad, string camposEncriptar)
        {
            var cliente = await CrearClienteConTokenAsync();
            var url = $"api/{tabla}/{campoClave}/{valor}?camposEncriptar={camposEncriptar}";
            var respuesta = await cliente.PutAsJsonAsync(url, entidad);
            await LanzarSiError(respuesta);
            return "Registro actualizado correctamente.";
        }

        public async Task<string> EliminarAsync(string tabla, string campoClave, object valor)
        {
            var cliente = await CrearClienteConTokenAsync();
            var respuesta = await cliente.DeleteAsync($"api/{tabla}/{campoClave}/{valor}");
            await LanzarSiError(respuesta);
            return "Registro eliminado correctamente.";
        }

        public async Task<string> EjecutarStoredProcedureAsync<T>(string nombreSP, T parametros)
        {
            var cliente = await CrearClienteConTokenAsync();
            var parametrosDict = new Dictionary<string, object?>();
            parametrosDict["nombreSP"] = nombreSP;
            var propiedades = typeof(T).GetProperties();
            foreach (var propiedad in propiedades)
            {
                var valor = propiedad.GetValue(parametros);
                parametrosDict[propiedad.Name] = valor;
            }
            var respuesta = await cliente.PostAsJsonAsync("api/procedimientos/ejecutarsp", parametrosDict);
            await LanzarSiError(respuesta);
            return "Stored procedure ejecutado correctamente.";
        }

        public async Task<RespuestaSP> EjecutarStoredProcedureConResultadosAsync<T>(string nombreSP, T parametros)
        {
            var cliente = await CrearClienteConTokenAsync();
            var parametrosDict = new Dictionary<string, object?>();
            parametrosDict["nombreSP"] = nombreSP;
            var propiedades = typeof(T).GetProperties();
            foreach (var propiedad in propiedades)
            {
                var valor = propiedad.GetValue(parametros);
                parametrosDict[propiedad.Name] = valor;
            }
            var respuesta = await cliente.PostAsJsonAsync("api/procedimientos/ejecutarsp", parametrosDict);
            await LanzarSiError(respuesta);
            var resultado = await respuesta.Content.ReadFromJsonAsync<RespuestaSP>();
            return resultado ?? new RespuestaSP();
        }

        public async Task<string> EjecutarStoredProcedureAsync<T>(string nombreSP, T parametros, string camposEncriptar)
        {
            var cliente = await CrearClienteConTokenAsync();
            var parametrosDict = new Dictionary<string, object?>();
            parametrosDict["nombreSP"] = nombreSP;
            if (parametros is Dictionary<string, object?> dict)
            {
                foreach (var kvp in dict)
                {
                    parametrosDict[kvp.Key] = kvp.Value;
                }
            }
            else
            {
                var propiedades = typeof(T).GetProperties();
                foreach (var propiedad in propiedades)
                {
                    var valor = propiedad.GetValue(parametros);
                    parametrosDict[propiedad.Name] = valor;
                }
            }
            var url = $"api/procedimientos/ejecutarsp?camposEncriptar={camposEncriptar}";
            var respuesta = await cliente.PostAsJsonAsync(url, parametrosDict);
            await LanzarSiError(respuesta);
            return "Stored procedure ejecutado correctamente.";
        }

        private class ApiRespuesta<T>
        {
            public int Estado { get; set; }
            public string? Mensaje { get; set; }
            public T? Datos { get; set; }
        }

        private class ApiError
        {
            public int Estado { get; set; }
            public string? Mensaje { get; set; }
        }
    }

    public class RespuestaSP
    {
        public string? Procedimiento { get; set; }
        public List<Dictionary<string, object>>? Resultados { get; set; }
        public int Total { get; set; }
        public string? Mensaje { get; set; }
    }
}
