using BodeGo.API.Operations.Application.Services;
using BodeGo.API.Operations.Domain.Exceptions;
using BodeGo.API.Operations.Domain.Model.Queries;
using BodeGo.API.Operations.Interfaces.REST.Resources;
using BodeGo.API.Operations.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;
namespace BodeGo.API.Operations.Interfaces.REST;
[ApiController]
[Tags("Operations")]
[Route("api/v1/inventory-counts")]
public class InventoryCountsController(IInventoryCountCommandService commands, IInventoryCountQueryService queries) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<InventoryCountResource>(201)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(409)]
    public async Task<IActionResult> Create(CreateInventoryCountResource resource, CancellationToken ct)
    {
        try
        {
            var count = await commands.Handle(InventoryCountResourceAssembler.ToCommand(resource), ct);
            return CreatedAtAction(nameof(GetById), new { id = count.Id }, InventoryCountResourceAssembler.From(count));
        }
        catch (OperationsException e) { return Problem(statusCode: e.Conflict ? 409 : 400, title: e.Message); }
    }
    [HttpGet("{id}")]
    [ProducesResponseType<InventoryCountResource>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            var count = await queries.Handle(new GetInventoryCountByIdQuery(id), ct);
            return count == null ? Problem(statusCode: 404, title: "No se encontró el conteo solicitado.")
                : Ok(InventoryCountResourceAssembler.From(count));
        }
        catch (OperationsException e) { return Problem(statusCode: 400, title: e.Message); }
    }
}
