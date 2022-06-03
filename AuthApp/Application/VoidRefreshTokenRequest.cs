using Application.Repositories;
using Data.Common.Contracts;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Application
{
    public record class VoidRefreshTokenRequest(Guid User, string token) : IRequest;

    public class VoidRefreshTokenRequestHandler : IRequestHandler<VoidRefreshTokenRequest>
    {
        private readonly IConfiguration _configuration;

        private readonly IAsyncRepository<RefreshToken> _refreshTokenRepository;

        public VoidRefreshTokenRequestHandler(
            IConfiguration configuration, 
            IAsyncRepository<RefreshToken> refreshTokenRepository)
        {
            _configuration = configuration;
            _refreshTokenRepository = refreshTokenRepository;
        }

        public async Task<Unit> Handle(VoidRefreshTokenRequest request, CancellationToken cancellationToken)
        {
            RefreshToken? existingToken = await _refreshTokenRepository.FindAsync(
                specs: new IRefreshTokenRepository.UserWithRefreshTokenSpecification(
                    User: request.User,
                    RefreshToken: request.token),
                token: cancellationToken);

            if (existingToken == null)
            {
                throw new ApplicationLogicException(message: "Invalid token.");
            }

            existingToken.Consume();

            await _refreshTokenRepository.SaveAsync(item: existingToken, token: cancellationToken);

            return Unit.Value;
        }
    }
}
