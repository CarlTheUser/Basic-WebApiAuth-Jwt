using Access;
using Data.Common.Contracts;
using Microsoft.Extensions.Configuration;

namespace Application.Authentication
{
    public class EmailPasswordAuthentication : IAuthentication<EmailPasswordAuthCredentials>
    {
        private readonly IConfiguration _configuration;
        private readonly IAsyncQuery<UserAccess?, string> _userAccessByEmailquery;

        public EmailPasswordAuthentication(IConfiguration configuration, IAsyncQuery<UserAccess?, string> userAccessByEmailquery)
        {
            _configuration = configuration;
            _userAccessByEmailquery = userAccessByEmailquery;
        }

        public async Task<AuthenticationResult> AuthenticateAsync(EmailPasswordAuthCredentials credentials, CancellationToken token)
        {
            try
            {
                UserAccess? userAccess = await _userAccessByEmailquery.ExecuteAsync(parameter: credentials.Email, token: token);

                if (userAccess != null)
                {
                    if (userAccess.Password.Test(password: _configuration["Application:Security:Authentication:Peanuts"] + new string(credentials.Password)))
                    {
                        return AuthenticationResult.Ok(
                            user: new AuthenticatedUser(
                                userAccess.Guid,
                                userAccess.Email,
                                userAccess.Role.Name));
                    }
                    else
                    {
                        return AuthenticationResult.InvalidCredentials(identifier: userAccess.Email);
                    }
                }
                else
                {
                    return AuthenticationResult.NotFound;
                }
            }
            finally
            {
                credentials.Flush();
            }
        }
    }
}
