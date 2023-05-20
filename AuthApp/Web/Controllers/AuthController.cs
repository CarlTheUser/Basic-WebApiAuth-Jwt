using Application;
using Application.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Models;

namespace Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ITokenService<EmailPasswordAuthCredentials> _tokenService;

        public AuthController(ITokenService<EmailPasswordAuthCredentials> tokenService)
        {
            _tokenService = tokenService;
        }

        [AllowAnonymous]
        [Route("Token")]
        [HttpPost]
        public async Task<IActionResult> Token(PasswordAuthTokenBindingModel model, CancellationToken cancellationToken = default)
        {
            Result<AuthenticateResponse?> authResult = await _tokenService.GetTokenAsync(
                credentials: new EmailPasswordAuthCredentials(
                    email: model.Email!,
                    password: model.Password!),
                cancellationToken: cancellationToken);

            if (authResult.Success)
            {
                AuthenticateResponse authenticateResponse = authResult.Value!;

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

            Response.ContentType = "application/problem+json";
            Response.StatusCode = StatusCodes.Status400BadRequest;

            return Unauthorized(new ProblemDetails()
            {
                Type = $"{Request.Scheme}://{Request.Host}/errors/unauthorized",
                Title = "Bad Request",
                Detail = authResult.Message,
                Instance = Request.Path,
                Status = Response.StatusCode
            });
        }

        [AllowAnonymous]
        [Route("Refresh")]
        [HttpPost]
        public async Task<IActionResult> Refresh(CancellationToken cancellationToken = default)
        {
            if (Request.Cookies.TryGetValue("X-Refresh-Token", out string? refreshToken)
                && Request.Cookies.TryGetValue("X-User-Id", out string? userIdString)
                && Guid.TryParse(userIdString, out Guid userId))
            {
                Result<AuthenticateResponse?> authResult = await _tokenService.RefreshTokenAsync(
                    userAccessId: userId,
                    refreshTokenCode: refreshToken!,
               cancellationToken: cancellationToken);

                if (authResult.Success)
                {
                    AuthenticateResponse authenticateResponse = authResult.Value!;

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
            }
            return Unauthorized();
        }

        [AllowAnonymous]
        [Route("Revoke")]
        [HttpPost]
        public async Task<IActionResult> Revoke(CancellationToken cancellationToken = default)
        {
            if (Request.Cookies.TryGetValue("X-Refresh-Token", out string? refreshToken)
                && Request.Cookies.TryGetValue("X-User-Id", out string? userIdString)
                && Guid.TryParse(userIdString, out Guid userId))
            {
                _ = await _tokenService.RevokeAsync(
                    userAccessId: userId,
                    refreshTokenCode: refreshToken!,
                    cancellationToken: cancellationToken);
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
