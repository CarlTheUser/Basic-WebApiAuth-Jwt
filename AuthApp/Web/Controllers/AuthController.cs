using Application;
using Application.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Models;

namespace Web.Controllers
{
    [Route("api/[controller]")]
    [TypeFilter(typeof(AuthExceptionFilter))]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [AllowAnonymous]
        [Route("Token")]
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

            AuthenticateResponse authenticateResponse = await _mediator.Send(
                new AuthenticateRequest(
                    new EmailPasswordAuthCredentials(
                        model.Username, 
                        model.Password)), 
                token);

            DateTime cookieExpiry = authenticateResponse.RefreshTokenExpiry;

            Response.Cookies.Append(
                "X-Refresh-Token",
                authenticateResponse.RefreshToken,
                new CookieOptions()
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.None,
                    Expires = cookieExpiry,
                    Secure = true
                });

            Response.Cookies.Append(
                "X-User-Id",
                authenticateResponse.User.ToString(),
                new CookieOptions()
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.None,
                    Expires = cookieExpiry,
                    Secure = true
                });

            return Ok(new
            {
                access_token = authenticateResponse.AccessToken
            });
        }

        [AllowAnonymous]
        [Route("Re")]
        [HttpPost]
        public async Task<IActionResult> Re(CancellationToken token)
        {
            if(Request.Cookies.TryGetValue("X-Refresh-Token", out string? refreshToken) 
                && Request.Cookies.TryGetValue("X-User-Id", out string? userIdString) 
                && Guid.TryParse(userIdString, out Guid userId))
            {
                AuthenticateResponse authenticateResponse = await _mediator.Send(
                    new RefreshTokenRequest(userId, refreshToken!),
                    token);

                DateTime cookieExpiry = authenticateResponse.RefreshTokenExpiry;

                Response.Cookies.Append(
                    "X-Refresh-Token",
                    authenticateResponse.RefreshToken,
                    new CookieOptions()
                    {
                        HttpOnly = true,
                        SameSite = SameSiteMode.None,
                        Expires = cookieExpiry,
                        Secure = true
                    });

                Response.Cookies.Append(
                    "X-User-Id",
                    authenticateResponse.User.ToString(),
                    new CookieOptions()
                    {
                        HttpOnly = true,
                        SameSite = SameSiteMode.None,
                        Expires = cookieExpiry,
                        Secure = true
                    });

                return Ok(new
                {
                    access_token = authenticateResponse.AccessToken
                });
            }
            return Unauthorized();
        }

        [AllowAnonymous]
        [Route("Revoke")]
        [HttpPost]
        public async Task<IActionResult> Revoke(CancellationToken token)
        {
            if (Request.Cookies.TryGetValue("X-Refresh-Token", out string? refreshToken)
                && Request.Cookies.TryGetValue("X-User-Id", out string? userIdString)
                && Guid.TryParse(userIdString, out Guid userId))
            {
                await _mediator.Send(
                    new VoidRefreshTokenRequest(userId, refreshToken!),
                    token);
            }

            CookieOptions options = new()
            {
                Expires = DateTime.Today.AddDays(-60),
                SameSite = SameSiteMode.None,
                Secure = true,
                HttpOnly = true,
                Path = HttpContext.Request.PathBase
            };

            Response.Cookies.Append(
                "X-Refresh-Token",
                "",
                options);

            Response.Cookies.Append(
                "X-User-Id",
                "",
                options);

            return Ok();
        }
    }
}
