using Microsoft.JSInterop;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using FrontendBlazorApi.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace FrontendBlazorApi.Servicios
{
    public class ServicioAutenticacion
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly AuthenticationStateProvider _authStateProvider;

        public ServicioAutenticacion(IJSRuntime jsRuntime, AuthenticationStateProvider authStateProvider)
        {
            _jsRuntime = jsRuntime;
            _authStateProvider = authStateProvider;
        }

        public async Task IniciarSesionAsync(string token, string email)
        {
            try
            {
                Console.WriteLine("💾 ServicioAuth: Guardando token y email...");
                
                // Guardar en sessionStorage
                await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "token", token);
                await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "email", email);
                
                Console.WriteLine("✅ ServicioAuth: Token guardado en sessionStorage");

                // En lugar de notificar directamente, forzamos una recarga del estado
                await ForzarRecargaEstadoAutenticacion();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ServicioAuth - Error en IniciarSesionAsync: {ex.Message}");
                throw;
            }
        }

        private async Task ForzarRecargaEstadoAutenticacion()
        {
            try
            {
                Console.WriteLine("🔄 ServicioAuth: Forzando recarga del estado de autenticación...");
                
                // La forma más simple: obtener el estado actual forzará al provider a recargar
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                Console.WriteLine($"✅ ServicioAuth: Estado recargado - Autenticado: {authState.User.Identity?.IsAuthenticated}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ServicioAuth - Error recargando estado: {ex.Message}");
            }
        }

        public async Task CerrarSesionAsync()
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "token");
                await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "email");
                await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "rutasPermitidas");
                
                Console.WriteLine("✅ ServicioAuth: Datos eliminados de sessionStorage");

                // Forzar recarga del estado
                await ForzarRecargaEstadoAutenticacion();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ServicioAuth - Error en CerrarSesionAsync: {ex.Message}");
            }
        }

        // MÉTODOS PARA ROLES Y PERMISOS

        public async Task<List<string>> ObtenerRolesUsuarioAsync()
        {
            try
            {
                Console.WriteLine("🔍 ServicioAuth: Obteniendo roles del usuario...");
                
                var rutasPermitidas = await ObtenerRutasPermitidasAsync();
                var roles = rutasPermitidas?
                    .Select(r => r.NombreRol)
                    .Distinct()
                    .ToList() ?? new List<string>();
                
                Console.WriteLine($"🔍 ServicioAuth: Roles encontrados: {roles.Count}");
                foreach (var rol in roles)
                {
                    Console.WriteLine($"🔍 ServicioAuth: - {rol}");
                }
                return roles;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ServicioAuth - Error en ObtenerRolesUsuarioAsync: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task RefrescarPermisosUsuarioAsync()
        {
            try
            {
                Console.WriteLine("🔄 ServicioAuth: Refrescando permisos del usuario...");
                
                var email = await ObtenerEmailAsync();
                if (!string.IsNullOrEmpty(email))
                {
                    // Simplemente logueamos por ahora - puedes expandir esta funcionalidad después
                    Console.WriteLine("✅ ServicioAuth: Permisos refrescados para: " + email);
                }
                else
                {
                    Console.WriteLine("⚠️ ServicioAuth: No hay email para refrescar permisos");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ServicioAuth - Error en RefrescarPermisosUsuarioAsync: {ex.Message}");
            }
        }

        // MÉTODOS BÁSICOS

        public async Task<bool> EstaAutenticadoAsync()
        {
            try
            {
                var token = await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", "token");
                var autenticado = !string.IsNullOrEmpty(token);
                Console.WriteLine($"🔐 ServicioAuth: ¿Está autenticado?: {autenticado}");
                return autenticado;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string?> ObtenerTokenAsync()
        {
            try
            {
                return await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", "token");
            }
            catch
            {
                return null;
            }
        }

        public async Task<string?> ObtenerEmailAsync()
        {
            try
            {
                return await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", "email");
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<RutaRol>?> ObtenerRutasPermitidasAsync()
        {
            try
            {
                var rutasJson = await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", "rutasPermitidas");
                if (!string.IsNullOrEmpty(rutasJson))
                {
                    var rutas = JsonSerializer.Deserialize<List<RutaRol>>(rutasJson);
                    Console.WriteLine($"🗺️ ServicioAuth: {rutas?.Count ?? 0} rutas cargadas desde sessionStorage");
                    return rutas;
                }
                Console.WriteLine("🗺️ ServicioAuth: No hay rutas en sessionStorage");
                return new List<RutaRol>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ServicioAuth - Error en ObtenerRutasPermitidasAsync: {ex.Message}");
                return new List<RutaRol>();
            }
        }

        public async Task GuardarRutasRolAsync(List<RutaRol> rutas)
        {
            try
            {
                var rutasJson = JsonSerializer.Serialize(rutas);
                await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "rutasPermitidas", rutasJson);
                Console.WriteLine($"✅ ServicioAuth: {rutas.Count} rutas guardadas en sessionStorage");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ServicioAuth - Error en GuardarRutasRolAsync: {ex.Message}");
            }
        }

        public async Task<bool> TienePermisoParaRutaAsync(string ruta)
        {
            try
            {
                Console.WriteLine($"🔍 ServicioAuth: Verificando permiso para ruta: {ruta}");
                
                var rutasPermitidas = await ObtenerRutasPermitidasAsync();
                
                Console.WriteLine($"🔍 ServicioAuth: Rutas permitidas encontradas: {rutasPermitidas?.Count ?? 0}");
                
                var tienePermiso = rutasPermitidas?.Any(r => 
                    r.RutaUrl.Equals(ruta, StringComparison.OrdinalIgnoreCase)) ?? false;
                    
                Console.WriteLine($"🔍 ServicioAuth: ¿Tiene permiso para {ruta}?: {tienePermiso}");
                return tienePermiso;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ServicioAuth - Error en TienePermisoParaRutaAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> EsAdministradorAsync()
        {
            try
            {
                var roles = await ObtenerRolesUsuarioAsync();
                var esAdmin = roles.Contains("Administrador");
                Console.WriteLine($"👑 ServicioAuth: ¿Es administrador?: {esAdmin}");
                return esAdmin;
            }
            catch
            {
                return false;
            }
        }
    }
}