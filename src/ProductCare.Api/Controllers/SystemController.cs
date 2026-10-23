using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductCare.Api.Authorization;

namespace ProductCare.Api.Controllers;

[ApiController]
[Route("api/v1/system")]
[Authorize(Policy = ProductCarePolicies.User)]
public sealed class SystemController : ControllerBase
{
    [HttpGet("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            status = "ok"
        });
    }
}