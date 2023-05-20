using Access.Models.Entities;
using Access.Repositories;
using Data.Common.Contracts;

namespace Application.Authentication
{
    public class EmailPasswordAuthentication : IAuthentication<EmailPasswordAuthCredentials>
    {
        private readonly IUserAccessRepository _userAccessRepository;
        private readonly IAsyncQuery<AuthenticatedUser?, Guid> _authenticatedUserByIdQuery;

        public EmailPasswordAuthentication(IUserAccessRepository userAccessRepository, IAsyncQuery<AuthenticatedUser?, Guid> authenticatedUserByIdQuery)
        {
            _userAccessRepository = userAccessRepository;
            _authenticatedUserByIdQuery = authenticatedUserByIdQuery;
        }

        public async Task<AuthenticationResult> AuthenticateAsync(EmailPasswordAuthCredentials credentials, CancellationToken cancellationToken = default)
        {
            try
            {
                UserAccess? userAccess = await _userAccessRepository.FindAsync(
                    specification: new UserAccessByEmailSpecification(Email: credentials.Email),
                    cancellationToken: cancellationToken);

                if (userAccess != null)
                {
                    if (userAccess.Password.Test(password: new string(credentials.Password)))
                    {
                        AuthenticatedUser? authenticatedUser = await _authenticatedUserByIdQuery.ExecuteAsync(
                            parameter: userAccess.Id,
                            cancellationToken: cancellationToken);

                        return AuthenticationResult.Ok(user: authenticatedUser!);
                    }
                    else
                    {
                        return AuthenticationResult.InvalidCredentials(identifier: userAccess.Email.Value);
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
