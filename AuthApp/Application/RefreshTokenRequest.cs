﻿using Access;
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
    public record class RefreshTokenRequest(Guid User, string token, HttpResponse Response) : IRequest<TokenResponse>;

    public record class RefreshTokenByUserValueParameter(Guid User, string Value);

    public class RefreshTokenRequestHandler : IRequestHandler<RefreshTokenRequest, TokenResponse>
    {
        private readonly IConfiguration _configuration;

        private readonly IAsyncQuery<UserAccess?, Guid> _userAccessByIdQuery;

        private readonly IAsyncRepository<Guid, RefreshToken> _refreshTokenRepository;

        private readonly IAsyncQuery<RefreshToken?, RefreshTokenByUserValueParameter> _refreshTokenByUserValueQuery;

        private readonly IRandomStringGenerator _stringGenerator;

        public RefreshTokenRequestHandler(
            IConfiguration configuration,
            IAsyncQuery<UserAccess?, Guid> userAccessByIdQuery, 
            IAsyncRepository<Guid, RefreshToken> refreshTokenRepository, 
            IAsyncQuery<RefreshToken?, RefreshTokenByUserValueParameter> refreshTokenByUserValueQuery, 
            IRandomStringGenerator stringGenerator)
        {
            _configuration = configuration;
            _userAccessByIdQuery = userAccessByIdQuery;
            _refreshTokenRepository = refreshTokenRepository;
            _refreshTokenByUserValueQuery = refreshTokenByUserValueQuery;
            _stringGenerator = stringGenerator;
        }

        public async Task<TokenResponse> Handle(RefreshTokenRequest request, CancellationToken cancellationToken)
        {
            UserAccess? user = await _userAccessByIdQuery.ExecuteAsync(request.User, cancellationToken);

            if (user == null)
            {
                throw new ApplicationException("Cannot find user.");
            }

            RefreshToken? existingToken = await _refreshTokenByUserValueQuery.ExecuteAsync(
                    new RefreshTokenByUserValueParameter(user.Guid, request.token),
                    cancellationToken);

            if (existingToken == null || existingToken.Expiry < DateTime.Now)
            {
                throw new ApplicationException("Invalid token.");
            }

            existingToken.Consume();

            await _refreshTokenRepository.SaveAsync(existingToken, cancellationToken);

            var refreshToken = RefreshToken.For(
                        user: user.Guid,
                        lifespan: TimeSpan.Parse(_configuration["Application:Security:Authentication:RefreshToken:Lifespan"]),
                        _stringGenerator,
                        _configuration.GetValue<int>("Application:Security:Authentication:RefreshToken:Length"));

            await _refreshTokenRepository.SaveAsync(refreshToken, cancellationToken);

            SecurityKey symmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Application:Security:Authentication:Jwt:SymmetricSecurityKey"]));
            SigningCredentials credentials = new(symmetricKey, SecurityAlgorithms.HmacSha256);

            List<Claim> claims = new()
            {
                new Claim(ClaimTypes.NameIdentifier, user.Guid.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.Name)
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
                user.Guid.ToString(),
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
        }
    }
}
