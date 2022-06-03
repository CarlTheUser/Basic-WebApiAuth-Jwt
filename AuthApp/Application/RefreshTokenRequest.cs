using Access;
using Application.Repositories;
using Data.Common.Contracts;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Misc.Utilities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Application
{
    public record class RefreshTokenRequest(Guid User, string Token) : IRequest<AuthenticateResponse>;

    public record class RefreshTokenByUserValueParameter(Guid User, string Value);

    public class RefreshTokenRequestHandler : IRequestHandler<RefreshTokenRequest, AuthenticateResponse>
    {
        private readonly IConfiguration _configuration;

        private readonly IAsyncRepository<UserAccess> _userAccessRepository;

        private readonly IAsyncRepository<RefreshToken> _refreshTokenRepository;

        private readonly IRandomStringGenerator _stringGenerator;

        public RefreshTokenRequestHandler(
            IConfiguration configuration,
            IAsyncRepository<UserAccess> userAccessRepository, 
            IAsyncRepository<RefreshToken> refreshTokenRepository, 
            IRandomStringGenerator stringGenerator)
        {
            _configuration = configuration;
            _userAccessRepository = userAccessRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _stringGenerator = stringGenerator;
        }

        public async Task<AuthenticateResponse> Handle(RefreshTokenRequest request, CancellationToken cancellationToken)
        {
            UserAccess? user = await _userAccessRepository.FindAsync(
                specs: new IUserAccessRepository.IdSpecification(Id: request.User),
                token: cancellationToken);

            if (user == null)
            {
                throw new ApplicationLogicException(message: "Cannot find user.");
            }

            RefreshToken? existingToken = await _refreshTokenRepository.FindAsync(
                    specs: new IRefreshTokenRepository.UserWithRefreshTokenSpecification(User: user.Guid, RefreshToken: request.Token),
                    token: cancellationToken);

            if (existingToken == null)
            {
                throw new ApplicationLogicException(message: "Invalid token.");
            }

            existingToken.Consume();

            await _refreshTokenRepository.SaveAsync(item: existingToken, token: cancellationToken);

            var refreshToken = RefreshToken.For(
                        user: user.Guid,
                        lifespan: TimeSpan.Parse(_configuration["Application:Security:Authentication:RefreshToken:Lifespan"]),
                        generator: _stringGenerator,
                        tokenLength: _configuration.GetValue<int>("Application:Security:Authentication:RefreshToken:Length"));

            await _refreshTokenRepository.SaveAsync(item: refreshToken, token: cancellationToken);

            SecurityKey symmetricKey = new SymmetricSecurityKey(key: Encoding.UTF8.GetBytes(_configuration["Application:Security:Authentication:Jwt:SymmetricSecurityKey"]));
            SigningCredentials credentials = new(key: symmetricKey, algorithm: SecurityAlgorithms.HmacSha256);

            List<Claim> claims = new()
            {
                new Claim(type: ClaimTypes.NameIdentifier, value: user.Guid.ToString()),
                new Claim(type: ClaimTypes.Email, value: user.Email),
                new Claim(type: ClaimTypes.Role, value: user.Role.Name)
            };

            DateTime now = DateTime.Now;

            DateTime accessTokenExpiry = now.Add(value: TimeSpan.FromMinutes(20));

            SecurityToken securityToken = new JwtSecurityToken(
                    issuer: _configuration["Application:Security:Authentication:Jwt:Issuer"],
                    audience: "This Api",
                    claims: claims,
                    expires: accessTokenExpiry,
                    signingCredentials: credentials);

            string encodedToken = new JwtSecurityTokenHandler().WriteToken(token: securityToken);

            return new AuthenticateResponse(
                User: user.Guid,
                AccessToken: encodedToken,
                AccessTokenExpiry: accessTokenExpiry,
                RefreshToken: refreshToken.Value,
                RefreshTokenExpiry: refreshToken.Expiry);
        }
    }
}
