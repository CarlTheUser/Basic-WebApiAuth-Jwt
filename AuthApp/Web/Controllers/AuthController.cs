using Application;
using Application.Authentication;
using Data.Common.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Misc.Utilities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Web.Models;

namespace Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        private readonly IAuthentication<EmailPasswordAuthCredentials> _authentication;

        private readonly IRandomStringGenerator _stringGenerator;

        private readonly IAsyncRepository<Guid, RefreshToken> _refreshTokenRepository;

        public AuthController(
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

        [HttpPost]
        public async Task<IActionResult> Token(AuthTokenBindingModel model, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(model.grant_type) || model.grant_type.ToUpper() != "PASSWORD")
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.Password))
            {
                return Unauthorized();
            }

            var result = await _authentication.AuthenticateAsync(
                   new EmailPasswordAuthCredentials(
                       model.Username,
                       model.Password), token);

            switch (result.Status)
            {
                case AuthenticationStatus.Ok:

                    AuthenticatedUser user = ((Application.Authentication.OkResult)result).User;

                    var refreshToken = RefreshToken.For(
                        user: user.Id,
                        lifespan: TimeSpan.Parse(_configuration[""]),
                        _stringGenerator,
                        _configuration.GetValue<int>(""));

                    await _refreshTokenRepository.SaveAsync(refreshToken, token);

                    SecurityKey symmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
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
                            issuer: _configuration["Jwt:Issuer"],
                            audience: _configuration["Jwt:Issuer"],
                            claims: claims,
                            expires: accessTokenExpiry,
                            signingCredentials: credentials);

                    string encodedToken = new JwtSecurityTokenHandler().WriteToken(securityToken);

                    return Ok(
                        new{
                            access_token = encodedToken,
                            access_token_expires = accessTokenExpiry,
                            refreshToken = refreshToken.Value,
                            refresh_token_expires = refreshToken.Expiry
                        });

                case AuthenticationStatus.NotFound:
                    return Unauthorized();
                case AuthenticationStatus.InvalidCredentials:
                    return Unauthorized();
            }

            return Unauthorized();
        }
    }
}
