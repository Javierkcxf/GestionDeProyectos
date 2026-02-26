
namespace webapicsharp.Modelos
{
    public class ConfiguracionJwt
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int DuracionMinutos { get; set; } = 30;
    }
}
