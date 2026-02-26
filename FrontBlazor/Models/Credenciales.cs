using System.ComponentModel.DataAnnotations;

namespace FrontendBlazorApi.Models
{
    public class Credenciales
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;
    }
}
