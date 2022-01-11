namespace Application.Authentication
{
    public interface IAuthentication<TCredentials> where TCredentials : IAuthCredentials
    {
        AuthenticationResult Authenticate(TCredentials credentials);
    }
}
