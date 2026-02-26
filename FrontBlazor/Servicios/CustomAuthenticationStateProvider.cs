using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FrontendBlazorApi.Servicios
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IJSRuntime _jsRuntime;

        public CustomAuthenticationStateProvider(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                Console.WriteLine("🔐 CustomAuth: Iniciando verificación de autenticación...");
                
                // Leer token de sessionStorage
                var token = await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", "token");
                
                if (string.IsNullOrEmpty(token))
                {
                    Console.WriteLine("🔐 CustomAuth: No se encontró token en sessionStorage - Usuario NO autenticado");
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                Console.WriteLine($"🔐 CustomAuth: Token encontrado, longitud: {token.Length}");

                // Verificar si el token es válido
                if (!EsTokenValido(token))
                {
                    Console.WriteLine("🔐 CustomAuth: Token inválido o expirado");
                    await LimpiarStorage();
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                // Decodificar el token JWT
                var handler = new JwtSecurityTokenHandler();
                
                if (!handler.CanReadToken(token))
                {
                    Console.WriteLine("🔐 CustomAuth: No se puede leer el token JWT");
                    await LimpiarStorage();
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                var jwtToken = handler.ReadJwtToken(token);
                var claims = jwtToken.Claims.ToList();
                
                // Agregar claim básico de nombre si no existe
                if (!claims.Any(c => c.Type == ClaimTypes.Name))
                {
                    claims.Add(new Claim(ClaimTypes.Name, jwtToken.Subject ?? "Usuario"));
                }

                // Crear identity
                var identity = new ClaimsIdentity(claims, "jwt");
                var user = new ClaimsPrincipal(identity);
                
                Console.WriteLine($"🔐 CustomAuth: Usuario AUTENTICADO: {identity.IsAuthenticated}");
                Console.WriteLine($"🔐 CustomAuth: Nombre: {identity.Name}");
                Console.WriteLine($"🔐 CustomAuth: Claims: {claims.Count}");

                return new AuthenticationState(user);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ CustomAuth - Error en GetAuthenticationStateAsync: {ex.Message}");
                await LimpiarStorage();
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        private bool EsTokenValido(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                    return false;

                var jwtToken = handler.ReadJwtToken(token);
                
                // Verificar expiración
                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                    Console.WriteLine($"🔐 CustomAuth: Token expirado - Valido hasta: {jwtToken.ValidTo}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔐 CustomAuth: Error validando token: {ex.Message}");
                return false;
            }
        }

        private async Task LimpiarStorage()
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "token");
                await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "email");
                await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "rutasPermitidas");
                Console.WriteLine("🔐 CustomAuth: Storage limpiado");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ CustomAuth - Error limpiando storage: {ex.Message}");
            }
        }
    }
}