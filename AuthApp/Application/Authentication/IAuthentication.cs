namespace Application.Authentication
{
    public interface IAuthentication<TCredentials> where TCredentials : IAuthCredentials
    {
        Task<AuthenticationResult> AuthenticateAsync(TCredentials credentials, CancellationToken token);
    }
}
