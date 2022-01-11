using Access;
using Data.Common.Contracts;
using Microsoft.Extensions.Configuration;

namespace Application.Authentication
{
    public class EmailPasswordAuthentication : IAuthentication<EmailPasswordAuthCredentials>
    {
        private readonly IConfiguration _configuration;
        private readonly IQuery<UserAccess?, string> _userAccessByEmailquery;

        public EmailPasswordAuthentication(IConfiguration configuration, IQuery<UserAccess?, string> userAccessByEmailquery)
        {
            _configuration = configuration;
            _userAccessByEmailquery = userAccessByEmailquery;
        }

        public AuthenticationResult Authenticate(EmailPasswordAuthCredentials credentials)
        {
            UserAccess? userAccess = _userAccessByEmailquery.Execute(credentials.Email);

            if (userAccess != null)
            {
                if(userAccess.Password.Test(_configuration["Application:Authentication:Peanuts"] + new string(credentials.Password)))
                {
                    return AuthenticationResult.Ok(
                        new AuthenticatedUser(
                            userAccess.Guid,
                            userAccess.Email,
                            userAccess.Role.Name));
                }
                else
                {
                    return AuthenticationResult.InvalidCredentials(userAccess.Email);
                }
            }
            else
            {
                return AuthenticationResult.NotFound;
            }
        }
    }
}
