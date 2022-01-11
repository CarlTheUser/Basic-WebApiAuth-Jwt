using System.ComponentModel.DataAnnotations;

namespace Web.Models
{
    public class AuthTokenBindingModel
    {
        public string? Username { get; set; } = string.Empty;
        public string? Password { get; set; } = string.Empty;
        public string? grant_type { get; set; } = string.Empty;
    }
}
