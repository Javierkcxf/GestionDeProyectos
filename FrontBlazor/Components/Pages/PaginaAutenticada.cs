using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Authorization;
using FrontendBlazorApi.Servicios;

namespace FrontendBlazorApi.Components;

public abstract class PaginaAutenticada : ComponentBase
{
    [Inject] protected ServicioAutenticacion ServicioAuth { get; set; } = default!;
    [Inject] protected NavigationManager Navigation { get; set; } = default!;
    [Inject] protected AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] protected IJSRuntime JSRuntime { get; set; } = default!;

    /// <summary>
    /// Permite a la página saber cuándo ya puede renderizar información privada.
    /// </summary>
    protected bool AutenticacionVerificada { get; private set; } = false;
    protected bool EstaCargando { get; private set; } = true;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await VerificarAutenticacion();
            EstaCargando = false;
            StateHasChanged();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task VerificarAutenticacion()
    {
        try
        {
            Console.WriteLine("🔐 PaginaAutenticada: Verificando autenticación en OnAfterRenderAsync...");
            
            // 1️⃣ Verificar token en sessionStorage directamente (más confiable durante prerender)
            var token = await JSRuntime.InvokeAsync<string?>("sessionStorage.getItem", "token");
            Console.WriteLine($"🔐 Token en sessionStorage: {!string.IsNullOrEmpty(token)}");

            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("❌ No hay token, redirigiendo a /login");
                Navigation.NavigateTo("/login", forceLoad: true);
                return;
            }

            // 2️⃣ Verificar autenticación con el provider (ahora debería funcionar)
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var usuario = authState.User;

            Console.WriteLine($"🔐 Usuario autenticado: {usuario.Identity?.IsAuthenticated}");
            Console.WriteLine($"🔐 Nombre: {usuario.Identity?.Name}");

            if (!usuario.Identity?.IsAuthenticated ?? true)
            {
                Console.WriteLine("❌ Usuario no autenticado según AuthStateProvider, redirigiendo a /login");
                Navigation.NavigateTo("/login", forceLoad: true);
                return;
            }

            // 3️⃣ Verificar permisos de ruta
            var rutaActual = new Uri(Navigation.Uri).AbsolutePath;
            var rutasPublicas = new[] { "/", "/login" };

            Console.WriteLine($"🔐 Ruta actual: {rutaActual}");

            if (!rutasPublicas.Contains(rutaActual, StringComparer.OrdinalIgnoreCase))
            {
                var tienePermiso = await ServicioAuth.TienePermisoParaRutaAsync(rutaActual);
                Console.WriteLine($"🔐 ¿Tiene permiso para {rutaActual}?: {tienePermiso}");

                if (!tienePermiso)
                {
                    Console.WriteLine($"❌ Sin permisos para {rutaActual}, redirigiendo a /inicio");
                    Navigation.NavigateTo("/inicio", forceLoad: true);
                    return;
                }
            }

            // 4️⃣ Éxito - habilitar la página
            AutenticacionVerificada = true;
            Console.WriteLine("✅ PaginaAutenticada: Autenticación y permisos verificados correctamente");

            // Página hija continúa su inicialización
            await OnAutenticacionVerificada();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💥 PaginaAutenticada - Error en VerificarAutenticacion: {ex.Message}");
            Navigation.NavigateTo("/login", forceLoad: true);
        }
    }

    /// <summary>
    /// Método que las páginas hijas implementan para cargar datos
    /// después de validar autenticación y permisos.
    /// </summary>
    protected virtual Task OnAutenticacionVerificada()
        => Task.CompletedTask;

    /// <summary>
    /// Método auxiliar para mostrar loading mientras se verifica la autenticación
    /// </summary>
    protected virtual RenderFragment MostrarLoading()
    {
        return builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "loading-container");
            builder.AddMarkupContent(2, "<div class='spinner-border text-primary' role='status'><span class='visually-hidden'>Cargando...</span></div><p>Verificando autenticación...</p>");
            builder.CloseElement();
        };
    }
}
