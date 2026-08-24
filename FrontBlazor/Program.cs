using FrontendBlazorApi.Servicios;
using FrontendBlazorApi.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Configurar autenticación con cookies (mantener esto si lo necesitas para otras partes)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/access-denied";
        options.Cookie.Name = "FrontendBlazorAuth";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization();

// Agregar acceso al HttpContext
builder.Services.AddHttpContextAccessor();

// Servicios de Razor Components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// HttpClient para API
builder.Services.AddHttpClient("Api", cliente =>
{
    var apiUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5031/";
    var timeoutSeconds = int.Parse(builder.Configuration["ApiSettings:TimeoutSeconds"] ?? "30");
    cliente.BaseAddress = new Uri(apiUrl);
    cliente.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    cliente.DefaultRequestHeaders.Add("User-Agent", "FrontendBlazorApi/1.0");
});

builder.Services.AddHttpClient("ApiGenerica", cliente =>
{
    var apiUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5031/";
    var timeoutSeconds = int.Parse(builder.Configuration["ApiSettings:TimeoutSeconds"] ?? "30");
    cliente.BaseAddress = new Uri(apiUrl);
    cliente.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    cliente.DefaultRequestHeaders.Add("User-Agent", "FrontendBlazorApi/1.0");
});

// REGISTRO CORREGIDO DE SERVICIOS DE AUTENTICACIÓN
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddScoped<ServicioAutenticacion>();
builder.Services.AddScoped<ServicioApiGenerico>();

// Autorización para Blazor
builder.Services.AddAuthorizationCore();

var app = builder.Build();

// Configuración del pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// Detrás de un proxy que termina TLS (Render, Azure App Service, etc.), la petición
// le llega al contenedor como HTTP plano; sin esto, UseHttpsRedirection() entra en
// un bucle infinito de redirecciones porque nunca ve la petición como HTTPS.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
app.UseHttpsRedirection();
app.UseStaticFiles();

// Middleware de autenticación y autorización
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// Mapeo de componentes
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.Run();