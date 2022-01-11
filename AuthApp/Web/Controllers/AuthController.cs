using Application.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Web.Models;

namespace Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthentication<EmailPasswordAuthCredentials> _authentication;

        public AuthController(IAuthentication<EmailPasswordAuthCredentials> authentication)
        {
            _authentication = authentication;
        }

        [HttpPost]
        public IActionResult Token(AuthTokenBindingModel model)
        {
            if (string.IsNullOrWhiteSpace(model.grant_type) || model.grant_type.ToUpper() != "PASSWORD")
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.Password))
            {
                return Unauthorized();
            }

            var result = _authentication.Authenticate(
                   new EmailPasswordAuthCredentials(
                       model.Username,
                       model.Password));

            switch (result.Status)
            {
                case AuthenticationStatus.Ok:

                    break;
                case AuthenticationStatus.NotFound:
                    return Unauthorized();
                case AuthenticationStatus.InvalidCredentials:
                    return Unauthorized();
            }

            return Unauthorized();
        }
    }
}
