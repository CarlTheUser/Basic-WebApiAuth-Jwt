namespace Web.Models
{
    public class PasswordAuthTokenBindingModel
    {
        public string? Email { get; init; } = string.Empty;
        public string? Password { get; init; } = string.Empty;
        public string? grant_type { get; init; } = string.Empty;
    }
}
