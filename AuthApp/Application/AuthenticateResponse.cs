namespace Application
{
    //public record class TokenResponse(
    //    string access_token, 
    //    DateTime access_token_expires,
    //    string refresh);
    public record class AuthenticateResponse(
        Guid User,
        string AccessToken,
        DateTime AccessTokenExpiry,
        string RefreshToken,
        DateTime RefreshTokenExpiry);
}
