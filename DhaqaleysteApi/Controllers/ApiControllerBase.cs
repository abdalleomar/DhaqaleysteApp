using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace DhaqaleysteApi.Controllers
{
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        /// <summary>The signed-in staff member's id (from the token's "sub" claim).</summary>
        protected int CurrentUserId =>
            int.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id) ? id : 0;

        protected string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();

        protected ObjectResult ProblemResult(int status, string detail) =>
            Problem(detail: detail, statusCode: status);

        protected ActionResult ValidationError(string field, string message)
        {
            ModelState.AddModelError(field, message);
            return ValidationProblem(ModelState);
        }
    }
}
