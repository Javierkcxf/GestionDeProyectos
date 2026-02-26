using System.Text.Json.Serialization;
using System.Text.Json;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace FrontendBlazorApi.Models
{

    public class Usuario
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Contrasena { get; set; } = string.Empty;
        public string? RutaAvatar { get; set; }
        public bool Activo { get; set; }
    }


    public class TipoProyecto
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
    }

    public class Estado
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
    }

    public class TipoProducto
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
    }

   public class Entregable
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFinPrevista { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public DateTime? FechaFinalizacion { get; set; }

    
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public int IdProductoRelacionado { get; set; } = 0;
}


    public class VariableEstrategica
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
    }
    public class TipoResponsable
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
    }
    public class Responsable
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Id { get; set; }
        public int IdTipoResponsable { get; set; }
        public int IdUsuario { get; set; }
        public string? Nombre { get; set; }

    }
    public class Proyecto
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Id { get; set; }
        public int? IdProyectoPadre { get; set; }
        public int IdResponsable { get; set; }
        public int IdTipoProyecto { get; set; }
        public string? Codigo { get; set; }
        public string? Titulo { get; set; }
        public string? Descripcion { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFinPrevista { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public DateTime? FechaFinalizacion { get; set; }
        public string? RutaLogo { get; set; }
    }
    public class Estado_Proyecto
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int IdProyecto { get; set; }
        public int IdEstado { get; set; }
    }
    public class Producto
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Id { get; set; }
        public int IdTipoProducto { get; set; }
        public string? Codigo { get; set; }
        public string? Titulo { get; set; }
        public string? Descripcion { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFinPrevista { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public DateTime? FechaFinalizacion { get; set; }
        public string? RutaLogo { get; set; }
    }
    public class Proyecto_Producto
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int IdProyecto { get; set; }
        public int IdProducto { get; set; }
        public DateTime? FechaAsociacion { get; set; }
    }
    public class Producto_Entregable
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int IdProducto { get; set; }
        public int IdEntregable { get; set; }
        public DateTime? FechaAsociacion { get; set; }
    }
    public class Responsable_Entregable
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int IdResponsable { get; set; }
        public int IdEntregable { get; set; }
        public DateTime? FechaAsociacion { get; set; }
    }
    public class Archivo
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Id { get; set; }
        public int IdUsuario { get; set; }
        public string? Ruta { get; set; }
        public string? Nombre { get; set; }
        public string? Tipo { get; set; }
        public DateTime? Fecha { get; set; }

    }
    public class Archivo_Entregable
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int IdArchivo { get; set; }
        public int IdEntregable { get; set; }
    }
    public class Actividad
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Id { get; set; }
        public int IdEntregable { get; set; }
        public string? Titulo { get; set; }
        public string? Descripcion { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFinPrevista { get; set; }
        public DateTime FechaModificacion { get; set; }
        public DateTime FechaFinalizacion { get; set; }
        public int Prioridad { get; set; }
        [Range(0, 100)]
        public int PorcentajeAvance { get; set; }

    }
    public class Presupuesto
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Id { get; set; }
        public int IdProyecto { get; set; }
        public decimal MontoSolicitado { get; set; }
        public decimal? MontoAprobado { get; set; }
        public int? PeriodoAnio { get; set; }
        public DateTime? FechaSolicitud { get; set; }
        public DateTime? FechaAprobacion { get; set; }
        public string? Estado { get; set; }
        public string? Observaciones { get; set; }
    }

    public class DistribucionPresupuesto
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Id { get; set; }
        public int IdPresupuestoPadre { get; set; }
        public int IdProyectoHijo { get; set; }
        public decimal MontoAsignado { get; set; }
    }
    public class EjecucionPresupuesto
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Id { get; set; }
        public int IdPresupuesto { get; set; }
        public int Anio { get; set; }
        public decimal? MontoPlaneado { get; set; }
        public decimal? MontoEjecutado { get; set; }
        public string? Observaciones { get; set; }
    }
    public class ObjetivoEstrategico
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Id { get; set; }
        public int IdVariable { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
    }
    public class MetaEstrategica
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Id { get; set; }
        public int IdObjetivo { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
    }
    public class Meta_Proyecto
    {
        public int IdMeta { get; set; }
        public int IdProyecto { get; set; }
        public DateTime? FechaAsociacion { get; set; }
    }
    public class Rol
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del rol es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Nombre { get; set; } = string.Empty;
    }

    // ========================================
    // MODELO: Ruta (para páginas/permisos)
    // ========================================
    public class Ruta
    {
        [Required(ErrorMessage = "La ruta es obligatoria")]
        [StringLength(100, ErrorMessage = "La ruta no puede exceder 100 caracteres")]
        public string RutaUrl { get; set; } = string.Empty;

        [Required(ErrorMessage = "La descripción es obligatoria")]
        [StringLength(255, ErrorMessage = "La descripción no puede exceder 255 caracteres")]
        public string Descripcion { get; set; } = string.Empty;
    }

    // ========================================
    // MODELO: Rol_Usuario (relación muchos a muchos)
    // ========================================
    public class Rol_Usuario
    {
        [Required(ErrorMessage = "El email del usuario es obligatorio")]
        public string FkEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "El ID del rol es obligatorio")]
        public int FkIdRol { get; set; }
    }

    // ========================================
// MODELO: RutaRol (relación muchos a muchos)
// ========================================
public class RutaRol
{
    [Required(ErrorMessage = "La ruta es obligatoria")]
    public string RutaUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre del rol es obligatorio")]
    public string NombreRol { get; set; } = string.Empty;
}

    // ========================================
    // MODELO EXTENDIDO: Usuario con Roles
    // Este modelo se usa para mostrar usuarios con sus roles asignados
    // ========================================
    public class UsuarioConRoles
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? RutaAvatar { get; set; }
        public bool Activo { get; set; }

        // Lista de roles asignados al usuario
        public List<RolAsignado>? Roles { get; set; }
    }

    // ========================================
    // MODELO: RolAsignado (para el listado de roles de un usuario)
    // ========================================
    public class RolAsignado
    {
        public int IdRol { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }

    // ========================================
    // MODELO EXTENDIDO: Rol con Rutas
    // Este modelo se usa para mostrar roles con sus rutas/permisos
    // ========================================
    public class RolConRutas
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;

        // Lista de rutas asignadas al rol
        public List<RutaAsignada>? Rutas { get; set; }
    }

    // ========================================
    // MODELO: RutaAsignada (para el listado de rutas de un rol)
    // ========================================
    public class RutaAsignada
    {
        public string RutaUrl { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
    }

    // ========================================
    // MODELO: DTOs para creación/actualización
    // ========================================
    
    // DTO para crear/actualizar usuario con roles
    public class UsuarioConRolesDto
    {
        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        public string Email { get; set; } = string.Empty;

        [StringLength(255, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 255 caracteres")]
        public string? Contrasena { get; set; }

        public string? RutaAvatar { get; set; }
        
        public bool Activo { get; set; } = true;

        [Required(ErrorMessage = "Debe seleccionar al menos un rol")]
        [MinLength(1, ErrorMessage = "Debe seleccionar al menos un rol")]
        public List<int> RolesIds { get; set; } = new();
    }

    // DTO para asignar rol a usuario
    public class AsignarRolDto
    {
        [Required(ErrorMessage = "El email del usuario es obligatorio")]
        public string EmailUsuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "El ID del rol es obligatorio")]
        public int IdRol { get; set; }
    }

    // DTO para asignar ruta a rol
    public class AsignarRutaDto
    {
        [Required(ErrorMessage = "La ruta es obligatoria")]
        public string Ruta { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre del rol es obligatorio")]
        public string NombreRol { get; set; } = string.Empty;
    }

    // Clase genérica para mapear la respuesta de la API
    public class RespuestaApi<T>
    {
        public T? Datos { get; set; }
    }

}