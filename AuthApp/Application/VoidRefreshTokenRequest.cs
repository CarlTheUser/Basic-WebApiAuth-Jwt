using Data.Common.Contracts;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Application
{
    public record class VoidRefreshTokenRequest(Guid User, string token) : IRequest;

    public class VoidRefreshTokenRequestHandler : IRequestHandler<VoidRefreshTokenRequest>
    {
        private readonly IConfiguration _configuration;

        private readonly IAsyncRepository<Guid, RefreshToken> _refreshTokenRepository;

        private readonly IAsyncQuery<RefreshToken?, RefreshTokenByUserValueParameter> _refreshTokenByUserValueQuery;

        public VoidRefreshTokenRequestHandler(IConfiguration configuration, IAsyncRepository<Guid, RefreshToken> refreshTokenRepository, IAsyncQuery<RefreshToken?, RefreshTokenByUserValueParameter> refreshTokenByUserValueQuery)
        {
            _configuration = configuration;
            _refreshTokenRepository = refreshTokenRepository;
            _refreshTokenByUserValueQuery = refreshTokenByUserValueQuery;
        }

        public async Task<Unit> Handle(VoidRefreshTokenRequest request, CancellationToken cancellationToken)
        {
            RefreshToken? existingToken = await _refreshTokenByUserValueQuery.ExecuteAsync(
                    new RefreshTokenByUserValueParameter(request.User, request.token),
                    cancellationToken);

            if (existingToken == null)
            {
                throw new ApplicationLogicException("Invalid token.");
            }

            existingToken.Consume();

            await _refreshTokenRepository.SaveAsync(existingToken, cancellationToken);

            return Unit.Value;
        }
    }
}
