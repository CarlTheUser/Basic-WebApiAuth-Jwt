using Access;
using Data.Common.Contracts;
using MediatR;
using Microsoft.Extensions.Configuration;
using Misc.Utilities;

namespace Application
{
    public record class RefreshTokenRequest(Guid User, string token) : IRequest<TokenResponse>;

    public class RefreshTokenRequestHandler : IRequestHandler<RefreshTokenRequest, TokenResponse>
    {
        private readonly IConfiguration _configuration;

        private readonly IAsyncRepository<Guid, UserAccess> _userAccessRepository;

        private readonly IAsyncRepository<Guid, RefreshToken> _refreshTokenRepository;

        private readonly IRandomStringGenerator _stringGenerator;

        public RefreshTokenRequestHandler(
            IConfiguration configuration, 
            IAsyncRepository<Guid, UserAccess> userAccessRepository, 
            IAsyncRepository<Guid, RefreshToken> refreshTokenRepository, 
            IRandomStringGenerator stringGenerator)
        {
            _configuration = configuration;
            _userAccessRepository = userAccessRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _stringGenerator = stringGenerator;
        }

        public Task<TokenResponse> Handle(RefreshTokenRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
