namespace Application
{
    public record class AuthenticatedUser(Guid Id, string Email, string Role);
}