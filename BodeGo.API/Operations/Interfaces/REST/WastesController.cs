using BodeGo.API.Operations.Application.Services;
using BodeGo.API.Operations.Domain.Exceptions;
using BodeGo.API.Operations.Interfaces.REST.Resources;
using BodeGo.API.Operations.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;
namespace BodeGo.API.Operations.Interfaces.REST;
[ApiController]
[Tags("Operations")]
[Route("api/v1/wastes")]
public class WastesController(IWasteCommandService commands) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<WasteResource>(201)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(409)]
    public async Task<IActionResult> Create(CreateWasteResource resource, CancellationToken ct)
    {
        try
        {
            var waste = await commands.Handle(WasteResourceAssembler.ToCommand(resource), ct);
            return Created("/api/v1/wastes", WasteResourceAssembler.From(waste));
        }
        catch (OperationsException e) { return Problem(statusCode: e.Conflict ? 409 : 400, title: e.Message); }
    }
}
