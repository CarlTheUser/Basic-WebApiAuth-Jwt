using Application.Authentication;
using Data.Common.Contracts;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Misc.Utilities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Application
{
    public record class AuthenticateRequest(EmailPasswordAuthCredentials Credentials, HttpResponse Response) : IRequest<TokenResponse>;

    public class AuthenticateRequestHandler : IRequestHandler<AuthenticateRequest, TokenResponse>
    {
        private readonly IConfiguration _configuration;

        private readonly IAuthentication<EmailPasswordAuthCredentials> _authentication;

        private readonly IRandomStringGenerator _stringGenerator;

        private readonly IAsyncRepository<Guid, RefreshToken> _refreshTokenRepository;

        public AuthenticateRequestHandler(
            IConfiguration configuration, 
            IAuthentication<EmailPasswordAuthCredentials> authentication, 
            IRandomStringGenerator stringGenerator, 
            IAsyncRepository<Guid, RefreshToken> refreshTokenRepository)
        {
            _configuration = configuration;
            _authentication = authentication;
            _stringGenerator = stringGenerator;
            _refreshTokenRepository = refreshTokenRepository;
        }

        public async Task<TokenResponse> Handle(AuthenticateRequest request, CancellationToken cancellationToken)
        {
            var result = await _authentication.AuthenticateAsync(request.Credentials, cancellationToken);

            switch (result.Status)
            {
                case AuthenticationStatus.Ok:

                    AuthenticatedUser user = ((OkResult)result).User;

                    var refreshToken = RefreshToken.For(
                        user: user.Id,
                        lifespan: TimeSpan.Parse(_configuration["Application:Security:Authentication:RefreshToken:Lifespan"]),
                        _stringGenerator,
                        _configuration.GetValue<int>("Application:Security:Authentication:RefreshToken:Length"));

                    await _refreshTokenRepository.SaveAsync(refreshToken, cancellationToken);

                    SecurityKey symmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Application:Security:Authentication:Jwt:SymmetricSecurityKey"]));
                    SigningCredentials credentials = new(symmetricKey, SecurityAlgorithms.HmacSha256);

                    List<Claim> claims = new()
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Role, user.Role)
                    };

                    DateTime now = DateTime.Now;

                    DateTime accessTokenExpiry = now.Add(TimeSpan.FromMinutes(20));

                    SecurityToken securityToken = new JwtSecurityToken(
                            issuer: _configuration["Application:Security:Authentication:Jwt:Issuer"],
                            audience: "This Api",
                            claims: claims,
                            expires: accessTokenExpiry,
                            signingCredentials: credentials);

                    string encodedToken = new JwtSecurityTokenHandler().WriteToken(securityToken);

                    DateTime cookieExpiry = refreshToken.Expiry.ToUniversalTime();

                    request.Response.Cookies.Append(
                        "X-Refresh-Token",
                        refreshToken.Value,
                        new CookieOptions()
                        {
                            HttpOnly = true,
                            SameSite = SameSiteMode.None,
                            Expires = cookieExpiry,
                            Secure = true
                        });

                    request.Response.Cookies.Append(
                        "X-User-Id",
                        user.Id.ToString(),
                        new CookieOptions()
                        {
                            HttpOnly = true,
                            SameSite = SameSiteMode.None,
                            Expires = cookieExpiry,
                            Secure = true
                        });

                    request.Response.Cookies.Append(
                       "X-Can-Refresh",
                       true.ToString(),
                       new CookieOptions()
                       {
                           HttpOnly = false,
                           SameSite = SameSiteMode.None,
                           Expires = cookieExpiry,
                           Secure = true
                       });

                    return new TokenResponse(encodedToken, accessTokenExpiry);

                case AuthenticationStatus.NotFound:
                    throw new ApplicationException("Account not found.");
                case AuthenticationStatus.InvalidCredentials:
                    throw new ApplicationException("Wrong password.");
                default:
                    throw new ApplicationException("Unhandled Authentication Status");
            }
        }
    }
    
}
