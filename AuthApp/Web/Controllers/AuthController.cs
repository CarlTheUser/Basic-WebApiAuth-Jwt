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

            TokenResponse tokenResponse = await _mediator.Send(
                new AuthenticateRequest(
                    new EmailPasswordAuthCredentials(model.Username, model.Password),
                    Response), 
                token);
            
            return Ok(tokenResponse);
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
                TokenResponse tokenResponse = await _mediator.Send(
                    new RefreshTokenRequest(userId, refreshToken!, Response),
                    token);

                return Ok(tokenResponse);
            }
            return BadRequest();
        }
    }
}
