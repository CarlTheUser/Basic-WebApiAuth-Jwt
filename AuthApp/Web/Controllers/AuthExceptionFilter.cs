using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

namespace Web.Controllers
{
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
