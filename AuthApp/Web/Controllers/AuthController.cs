using Application;
using Application.Authentication;
using Data.Common.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;
using Misc.Utilities;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
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
        [TypeFilter(typeof(AuthExceptionFilter))]
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
                    Response));
            
            return Ok(tokenResponse);
        }

        [Authorize]
        [Route("Re")]
        [HttpPost]
        public async Task<IActionResult> Re(CancellationToken token)
        {
            if(Request.Cookies.TryGetValue("X-Refresh-Token", out string? refreshToken) && Request.Cookies.TryGetValue("X-User-Id", out string? userId))
            {

            }

            return Unauthorized();
        }

        public class AuthExceptionFilter : IExceptionFilter
        {
            public void OnException(ExceptionContext context)
            {
                switch (context.Exception)
                {
                    case Application.ApplicationException ae:
                        context.Result = new UnauthorizedObjectResult(new { ae.Message });
                        break;
                    default:
                        context.Result = new StatusCodeResult((int)HttpStatusCode.InternalServerError);
                        break;
                }
            }
        }
    }
}
