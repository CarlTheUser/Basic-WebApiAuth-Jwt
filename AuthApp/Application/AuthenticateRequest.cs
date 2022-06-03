using Application.Authentication;
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
    public record class AuthenticateRequest(EmailPasswordAuthCredentials Credentials) : IRequest<AuthenticateResponse>;

    public class AuthenticateRequestHandler : IRequestHandler<AuthenticateRequest, AuthenticateResponse>
    {
        private readonly IConfiguration _configuration;

        private readonly IAuthentication<EmailPasswordAuthCredentials> _authentication;

        private readonly IRandomStringGenerator _stringGenerator;

        private readonly IAsyncRepository<RefreshToken> _refreshTokenRepository;

        public AuthenticateRequestHandler(
            IConfiguration configuration, 
            IAuthentication<EmailPasswordAuthCredentials> authentication, 
            IRandomStringGenerator stringGenerator, 
            IAsyncRepository<RefreshToken> refreshTokenRepository)
        {
            _configuration = configuration;
            _authentication = authentication;
            _stringGenerator = stringGenerator;
            _refreshTokenRepository = refreshTokenRepository;
        }

        public async Task<AuthenticateResponse> Handle(AuthenticateRequest request, CancellationToken cancellationToken)
        {
            var result = await _authentication.AuthenticateAsync(request.Credentials, cancellationToken);

            switch (result.Status)
            {
                case AuthenticationStatus.Ok:

                    AuthenticatedUser user = ((OkResult)result).User;

                    var refreshToken = RefreshToken.For(
                        user: user.Id,
                        lifespan: TimeSpan.Parse(_configuration["Application:Security:Authentication:RefreshToken:Lifespan"]),
                        generator: _stringGenerator,
                        tokenLength: _configuration.GetValue<int>("Application:Security:Authentication:RefreshToken:Length"));

                    await _refreshTokenRepository.SaveAsync(item: refreshToken, token: cancellationToken);

                    SecurityKey symmetricKey = new SymmetricSecurityKey(key: Encoding.UTF8.GetBytes(_configuration["Application:Security:Authentication:Jwt:SymmetricSecurityKey"]));
                    SigningCredentials credentials = new(key: symmetricKey, algorithm: SecurityAlgorithms.HmacSha256);

                    List<Claim> claims = new()
                    {
                        new Claim(type: ClaimTypes.NameIdentifier, value: user.Id.ToString()),
                        new Claim(type: ClaimTypes.Email, value: user.Email),
                        new Claim(type: ClaimTypes.Role, value: user.Role)
                    };

                    DateTime now = DateTime.Now;

                    if(!TimeSpan.TryParse("Application:Security:Authentication:Jwt:Lifespan", out TimeSpan tokenLifespan))
                    {
                        throw new ApplicationLogicException(message: "Missing or invalid configuration @ Application:Security:Authentication:Jwt:Lifespan.");
                    }

                    DateTime accessTokenExpiry = now.Add(value: tokenLifespan);

                    SecurityToken securityToken = new JwtSecurityToken(
                            issuer: _configuration["Application:Security:Authentication:Jwt:Issuer"],
                            audience: "This Api",
                            claims: claims,
                            expires: accessTokenExpiry,
                            signingCredentials: credentials);

                    string encodedToken = new JwtSecurityTokenHandler().WriteToken(token: securityToken);

                    return new AuthenticateResponse(
                        User: user.Id,
                        AccessToken: encodedToken,
                        AccessTokenExpiry: accessTokenExpiry,
                        RefreshToken: refreshToken.Value,
                        RefreshTokenExpiry: refreshToken.Expiry);

                case AuthenticationStatus.NotFound:
                    throw new ApplicationLogicException(message: "Account not found.");
                case AuthenticationStatus.InvalidCredentials:
                    throw new ApplicationLogicException(message: "Wrong password.");
                default:
                    throw new ApplicationLogicException(message: "Unhandled Authentication Status");
            }
        }
    }
    
}
