using Access.Models.Entities;
using Access.Models.Primitives;
using Access.Repositories;
using Application.Authentication;
using Data.Common.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Misc.Utilities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Application
{
    public interface ITokenService<TCredentials> where TCredentials : IAuthCredentials
    {
        Task<Result<AuthenticateResponse?>> GetTokenAsync(TCredentials credentials, CancellationToken cancellationToken = default);
        Task<Result<AuthenticateResponse?>> RefreshTokenAsync(Guid userAccessId, string refreshTokenCode, CancellationToken cancellationToken = default);
        Task<Result> RevokeAsync(Guid userAccessId, string refreshTokenCode, CancellationToken cancellationToken = default);
    }

    public class TokenService : ITokenService<EmailPasswordAuthCredentials>
    {
        private readonly IConfiguration _configuration;
        private readonly IAuthentication<EmailPasswordAuthCredentials> _authentication;
        private readonly IRandomStringGenerator _randomStringGenerator;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IUserAccessRepository _userAccessRepository;
        private readonly IAsyncQuery<AuthenticatedUser?, Guid> _authenticatedUserByIdQuery;

        public TokenService(
            IConfiguration configuration,
            IAuthentication<EmailPasswordAuthCredentials> authentication,
            IRandomStringGenerator randomStringGenerator,
            IRefreshTokenRepository refreshTokenRepository,
            IUserAccessRepository userAccessRepository,
            IAsyncQuery<AuthenticatedUser?, Guid> authenticatedUserByIdQuery)
        {
            _configuration = configuration;
            _authentication = authentication;
            _randomStringGenerator = randomStringGenerator;
            _refreshTokenRepository = refreshTokenRepository;
            _userAccessRepository = userAccessRepository;
            _authenticatedUserByIdQuery = authenticatedUserByIdQuery;
        }

        public async Task<Result<AuthenticateResponse?>> GetTokenAsync(EmailPasswordAuthCredentials credentials, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await _authentication.AuthenticateAsync(credentials, cancellationToken);

            switch (result.Status)
            {
                case AuthenticationStatus.Ok:

                    AuthenticatedUser user = ((OkResult)result).User;

                    var refreshToken = RefreshToken.For(
                        userAccess: user.Id,
                        lifespan: _configuration.GetValue<TimeSpan>(key: "Application:Security:Authentication:RefreshToken:Lifespan"),
                        generator: _randomStringGenerator,
                        tokenLength: _configuration.GetValue<int>("Application:Security:Authentication:RefreshToken:Length"));

                    await _refreshTokenRepository.SaveAsync(refreshToken, cancellationToken);

                    SecurityKey symmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Application:Security:Authentication:Jwt:SymmetricSecurityKey"]));
                    SigningCredentials signingCredentials = new(symmetricKey, SecurityAlgorithms.HmacSha256);

                    List<Claim> claims = new()
                    {
                        new Claim("id", user.Id.ToString()),
                        new Claim("email", user.Email),
                        new Claim("role", user.Role)
                    };

                    DateTime now = DateTime.Now;

                    DateTime accessTokenExpiry = now.Add(_configuration.GetValue<TimeSpan>(key: "Application:Security:Authentication:Jwt:Lifespan"));

                    SecurityToken securityToken = new JwtSecurityToken(
                            issuer: _configuration["Application:Security:Authentication:Jwt:Issuer"],
                            audience: _configuration["Application:Security:Authentication:Jwt:Audience"],
                            claims: claims,
                            expires: accessTokenExpiry,
                            signingCredentials: signingCredentials);

                    string encodedToken = new JwtSecurityTokenHandler().WriteToken(securityToken);

                    return Result.Ok<AuthenticateResponse?>(new AuthenticateResponse(
                        User: user.Id,
                        AccessToken: encodedToken,
                        AccessTokenExpiry: accessTokenExpiry,
                        RefreshToken: refreshToken.Code.Value,
                        RefreshTokenExpiry: refreshToken.Expiry));

                case AuthenticationStatus.NotFound:
                    return Result.Fail<AuthenticateResponse?>("Account not found.");
                case AuthenticationStatus.InvalidCredentials:
                    return Result.Fail<AuthenticateResponse?>("Wrong password.");
                default:
                    return Result.Fail<AuthenticateResponse?>("Unhandled Authentication Status");

            }
        }

        public async Task<Result<AuthenticateResponse?>> RefreshTokenAsync(Guid userAccessId, string refreshTokenCode, CancellationToken cancellationToken = default)
        {
            UserAccess? user = await _userAccessRepository.FindAsync(
                specification: new UserAccessByUserAccessIdSpecification(userAccessId),
                cancellationToken: cancellationToken);

            if (user == null)
            {
                return Result.Fail<AuthenticateResponse?>("Cannot find user.");
            }

            RefreshToken? existingToken = await _refreshTokenRepository.FindAsync(
                specification: new RefreshTokenByIssuedToAndTokenCodeSpecification(
                    IssuedTo: userAccessId,
                    TokenCode: refreshTokenCode),
                cancellationToken: cancellationToken);

            if (existingToken == null)
            {
                return Result.Fail<AuthenticateResponse?>("Invalid token.");
            }

            existingToken.Consume();

            await _refreshTokenRepository.SaveAsync(existingToken, cancellationToken);

            var refreshToken = RefreshToken.For(
                userAccess: user.Id,
                lifespan: _configuration.GetValue<TimeSpan>(key: "Application:Security:Authentication:RefreshToken:Lifespan"),
                generator: _randomStringGenerator,
                tokenLength: _configuration.GetValue<int>("Application:Security:Authentication:RefreshToken:Length"));

            await _refreshTokenRepository.SaveAsync(refreshToken, cancellationToken);

            var authenticatedUser = await _authenticatedUserByIdQuery.ExecuteAsync(user.Id, cancellationToken);

            if (authenticatedUser == null)
            {
                return Result.Fail<AuthenticateResponse?>("An error occurred while finding user.");
            }
            var symmetricKey = new SymmetricSecurityKey(key: Encoding.UTF8.GetBytes(s: _configuration["Application:Security:Authentication:Jwt:SymmetricSecurityKey"]));
            var credentials = new SigningCredentials(
                key: symmetricKey,
                algorithm: SecurityAlgorithms.HmacSha256);

            List<Claim> claims = new()
            {
                new Claim("id", authenticatedUser.Id.ToString()),
                new Claim("email", authenticatedUser.Email),
                new Claim("role", authenticatedUser.Role)
            };

            DateTime now = DateTime.Now;

            DateTime accessTokenExpiry = now.Add(_configuration.GetValue<TimeSpan>("Application:Security:Authentication:Jwt:Lifespan"));

            SecurityToken securityToken = new JwtSecurityToken(
                    issuer: _configuration["Application:Security:Authentication:Jwt:Issuer"],
                    audience: _configuration["Application:Security:Authentication:Jwt:Audience"],
                    claims: claims,
                    expires: accessTokenExpiry,
                    signingCredentials: credentials);

            string encodedToken = new JwtSecurityTokenHandler().WriteToken(securityToken);

            return Result.Ok<AuthenticateResponse?>(new AuthenticateResponse(
                User: user.Id,
                AccessToken: new TokenCode(Value: encodedToken),
                AccessTokenExpiry: accessTokenExpiry,
                RefreshToken: refreshToken.Code,
                RefreshTokenExpiry: refreshToken.Expiry));
        }

        public async Task<Result> RevokeAsync(Guid userAccessId, string refreshTokenCode, CancellationToken cancellationToken = default)
        {
            RefreshToken? existingToken = await _refreshTokenRepository.FindAsync(
                specification: new RefreshTokenByIssuedToAndTokenCodeSpecification(
                    IssuedTo: userAccessId,
                    TokenCode: refreshTokenCode)
                , cancellationToken);

            if (existingToken == null)
            {
                return Result.Fail("Invalid token.");
            }

            existingToken.Consume();

            await _refreshTokenRepository.SaveAsync(existingToken, cancellationToken);

            return Result.Ok();
        }
    }
}
