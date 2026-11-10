using BodeGo.API.Operations.Application.Services;
using BodeGo.API.Operations.Domain.Model.Queries;
using BodeGo.API.Operations.Interfaces.REST.Resources;
using BodeGo.API.Operations.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;
namespace BodeGo.API.Operations.Interfaces.REST;
[ApiController]
[Tags("Operations")]
[Route("api/v1/inventory-movements")]
public class InventoryMovementsController(IInventoryMovementQueryService queries) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IEnumerable<InventoryMovementResource>>(200)]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok((await queries.Handle(new GetInventoryMovementsQuery(), ct)).Select(InventoryMovementResourceAssembler.From));
}
