using Korone.RccServiceArbiter.Rendering;
using Microsoft.AspNetCore.Mvc;
using Roblox.Rendering;
using Roblox.Web.Infrastructure.Metadata;

namespace Korone.RccServiceArbiter.Controllers;

[ApiController]
[InternalServiceOnly]
[Route("")]
public sealed class RenderController(IRenderService renderer) : ControllerBase
{
    [HttpPost("render")]
    public async Task<ActionResult<RenderResult>> Render([FromBody] RenderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await renderer.RenderAsync(request, cancellationToken));
        }
        catch (RenderValidationException ex) { return Error(StatusCodes.Status400BadRequest, ex.Message); }
        catch (RenderCapacityException ex) { return Error(StatusCodes.Status429TooManyRequests, ex.Message); }
        catch (TimeoutException ex) { return Error(StatusCodes.Status504GatewayTimeout, ex.Message); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) { return Error(StatusCodes.Status502BadGateway, ex.Message); }
    }

    [HttpGet("render/statistics")]
    public ActionResult<RenderStatistics> Statistics() => Ok(renderer.GetStatistics());

    private ObjectResult Error(int status, string message) => StatusCode(status,
        new RenderErrorResponse { Errors = [new RenderError { Code = 0, Message = message }] });
}
