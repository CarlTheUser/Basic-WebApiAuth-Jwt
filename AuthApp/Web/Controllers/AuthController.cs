using Application;
using Application.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Models;

namespace Web.Controllers
{
    [Route("api/[controller]")]
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
                Response.ContentType = "application/problem+json";
                Response.StatusCode = StatusCodes.Status401Unauthorized;

                return Unauthorized(new ProblemDetails()
                {
                    Type = $"{Request.Scheme}://{Request.Host}/errors/unauthorized-grant-type",
                    Title = "Unauthorized",
                    Detail = "Unsupported grant_type.",
                    Instance = Request.Path,
                    Status = Response.StatusCode
                });
            }

            if (string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.Password))
            {
                Response.ContentType = "application/problem+json";
                Response.StatusCode = StatusCodes.Status401Unauthorized;

                return Unauthorized(new ProblemDetails()
                {
                    Type = $"{Request.Scheme}://{Request.Host}/errors/unauthorized-credentials",
                    Title = "Unauthorized",
                    Detail = "Unsupported or incomplete credentials",
                    Instance = Request.Path,
                    Status = Response.StatusCode
                });
            }

            AuthenticateResponse authenticateResponse = await _mediator.Send(
                request: new AuthenticateRequest(
                    Credentials: new EmailPasswordAuthCredentials(
                        email: model.Username, 
                        password: model.Password)), 
                token);

            DateTime cookieExpiry = authenticateResponse.RefreshTokenExpiry;

            Response.Cookies.Append(
                key: "X-Refresh-Token",
                value: authenticateResponse.RefreshToken,
                options: new CookieOptions()
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.None,
                    Expires = cookieExpiry,
                    Secure = true
                });

            Response.Cookies.Append(
                key: "X-User-Id",
                value: authenticateResponse.User.ToString(),
                options: new CookieOptions()
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
                    key: "X-Refresh-Token",
                    value: authenticateResponse.RefreshToken,
                    options: new CookieOptions()
                    {
                        HttpOnly = true,
                        SameSite = SameSiteMode.None,
                        Expires = cookieExpiry,
                        Secure = true
                    });

                Response.Cookies.Append(
                    key: "X-User-Id",
                    value: authenticateResponse.User.ToString(),
                    options: new CookieOptions()
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
                HttpOnly = true
            };

            Response.Cookies.Append(
                key: "X-Refresh-Token",
                value: string.Empty,
                options: options);

            Response.Cookies.Append(
                key: "X-User-Id",
                value: string.Empty,
                options: options); ;

            return Ok();
        }
    }
}
