
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using webapicsharp.Modelos; // donde está ConfiguracionJwt
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile(
    "tablasprohibidas.json",
    optional: true,
    reloadOnChange: true
);
builder.Services.AddControllers();
builder.Services.AddCors(opts =>
{
    opts.AddPolicy("PermitirTodo", politica => politica
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()
    );
});
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opciones =>
{
    opciones.IdleTimeout = TimeSpan.FromMinutes(30);
    opciones.Cookie.HttpOnly = true;
    opciones.Cookie.IsEssential = true;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opciones =>
{
    opciones.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "API Genérica CRUD Multi-Base de Datos",
        Version = "v1",
        Description = "API REST genérica para operaciones CRUD sobre cualquier tabla. Soporta SQL Server, PostgreSQL, MySQL y MariaDB. Incluye autenticación JWT y acceso dinámico configurado por tabla."
    });
    var esquemaSeguridad = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Ingrese el token con el prefijo 'Bearer'. Ejemplo: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    };
    opciones.AddSecurityDefinition("Bearer", esquemaSeguridad);
    opciones.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});
builder.Services.AddSingleton<
    webapicsharp.Servicios.Abstracciones.IPoliticaTablasProhibidas,
    webapicsharp.Servicios.Politicas.PoliticaTablasProhibidasDesdeJson>();
builder.Services.AddScoped<
    webapicsharp.Servicios.Abstracciones.IServicioCrud,
    webapicsharp.Servicios.ServicioCrud>();
builder.Services.AddSingleton<
    webapicsharp.Servicios.Abstracciones.IProveedorConexion,
    webapicsharp.Servicios.Conexion.ProveedorConexion>();
var proveedorBD = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "SqlServer";
builder.Services.AddScoped<webapicsharp.Servicios.Abstracciones.IServicioConsultas,
    webapicsharp.Servicios.ServicioConsultas>();
switch (proveedorBD.ToLower())
{
    case "postgres":
                builder.Services.AddScoped<webapicsharp.Repositorios.Abstracciones.IRepositorioLecturaTabla,
                                           webapicsharp.Repositorios.RepositorioLecturaPostgreSQL>();
        builder.Services.AddScoped<
            webapicsharp.Repositorios.Abstracciones.IRepositorioConsultas,
            webapicsharp.Repositorios.RepositorioConsultasPostgreSQL
        >();
        break;
    case "mariadb":
    case "mysql":
        builder.Services.AddScoped<
            webapicsharp.Repositorios.Abstracciones.IRepositorioLecturaTabla,
            webapicsharp.Repositorios.RepositorioLecturaMysqlMariaDB>();
        builder.Services.AddScoped<
            webapicsharp.Repositorios.Abstracciones.IRepositorioConsultas,
            webapicsharp.Repositorios.RepositorioConsultasMysqlMariaDB>();
        break;
    case "sqlserver":
    case "sqlserverexpress":
    case "localdb":
    default:
        builder.Services.AddScoped<webapicsharp.Repositorios.Abstracciones.IRepositorioLecturaTabla,
                                   webapicsharp.Repositorios.RepositorioLecturaSqlServer>();
        builder.Services.AddScoped<webapicsharp.Repositorios.Abstracciones.IRepositorioConsultas,
                               webapicsharp.Repositorios.RepositorioConsultasSqlServer>();
        break;
}
builder.Services.Configure<ConfiguracionJwt>(
    builder.Configuration.GetSection("Jwt")
);
var configuracionJwt = new ConfiguracionJwt();
builder.Configuration.GetSection("Jwt").Bind(configuracionJwt);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, // Valida el emisor del token.
            ValidateAudience = true, // Valida el público objetivo.
            ValidateLifetime = true, // Valida que no esté expirado.
            ValidateIssuerSigningKey = true, // Valida la firma.
            ValidIssuer = configuracionJwt.Issuer, // Emisor válido.
            ValidAudience = configuracionJwt.Audience, // Audiencia válida.
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuracionJwt.Key) // Clave secreta.
            )
        };
    });
builder.Services.Configure<ConfiguracionJwt>(builder.Configuration.GetSection("Jwt"));
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "webapicsharp v1");
    c.RoutePrefix = "swagger";
});
// Detrás de un proxy que termina TLS (Render, Azure App Service, etc.), la petición
// le llega al contenedor como HTTP plano; sin esto, UseHttpsRedirection() entra en
// un bucle infinito de redirecciones porque nunca ve la petición como HTTPS.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
app.UseHttpsRedirection();
app.UseCors("PermitirTodo");
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
