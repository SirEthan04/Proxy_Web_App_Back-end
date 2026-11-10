using BodeGo.API.Operations.Application.Services;
using BodeGo.API.Operations.Domain.Exceptions;
using BodeGo.API.Operations.Interfaces.REST.Resources;
using BodeGo.API.Operations.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;
namespace BodeGo.API.Operations.Interfaces.REST;
[ApiController]
[Tags("Operations")]
[Route("api/v1/wastes")]
public class WastesController(IWasteCommandService commands, IWasteQueryService queries) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IEnumerable<WasteResource>>(200)]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok((await queries.Handle(new BodeGo.API.Operations.Domain.Model.Queries.GetWastesQuery(), ct))
            .Select(WasteResourceAssembler.From));

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
