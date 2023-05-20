namespace Application
{
    public record class AuthenticateResponse(
        Guid User,
        string AccessToken,
        DateTime AccessTokenExpiry,
        string RefreshToken,
        DateTime RefreshTokenExpiry);
}
