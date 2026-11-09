using BodeGo.API.Commercial.Application.Services;
using BodeGo.API.Commercial.Domain.Model.Commands;
using BodeGo.API.Commercial.Domain.Model.Queries;
using BodeGo.API.Commercial.Interfaces.REST.Resources;
using BodeGo.API.Commercial.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;
namespace BodeGo.API.Commercial.Interfaces.REST;
[ApiController][Tags("Commercial")][Route("api/v1/purchases")]
public class PurchasesController(IPurchaseCommandService commands, IPurchaseQueryService queries) : ControllerBase
{
    [HttpPost][ProducesResponseType<CommercialResponse>(201)][ProducesResponseType<ProblemDetails>(400)]
    public async Task<IActionResult> Create(CreateCommercialResource resource,CancellationToken ct) { try { var r=await commands.Handle(new CreatePurchaseCommand(resource.UserId!.Value,CommercialAssembler.ToCommands(resource.Items)),ct); return CreatedAtAction(nameof(GetById),new{id=r.Id},CommercialAssembler.From(r)); } catch(ArgumentException e){return Problem(statusCode:400,title:e.Message);} }
    [HttpGet][ProducesResponseType<IEnumerable<CommercialResponse>>(200)] public async Task<IActionResult> GetAll(CancellationToken ct)=>Ok((await queries.Handle(new GetPurchasesQuery(),ct)).Select(CommercialAssembler.From));
    [HttpGet("{id}")][ProducesResponseType<CommercialResponse>(200)][ProducesResponseType<ProblemDetails>(400)][ProducesResponseType<ProblemDetails>(404)]
    public async Task<IActionResult> GetById(Guid id,CancellationToken ct){try{var r=await queries.Handle(new GetPurchaseByIdQuery(id),ct);return r is null?Problem(statusCode:404,title:"No se encontró la compra solicitada."):Ok(CommercialAssembler.From(r));}catch(ArgumentException e){return Problem(statusCode:400,title:e.Message);}}
}
[ApiController][Tags("Commercial")][Route("api/v1/sales")]
public class SalesController(ISaleCommandService commands, ISaleQueryService queries) : ControllerBase
{
    [HttpPost][ProducesResponseType<CommercialResponse>(201)][ProducesResponseType<ProblemDetails>(400)]
    public async Task<IActionResult> Create(CreateCommercialResource resource,CancellationToken ct) { try { var r=await commands.Handle(new CreateSaleCommand(resource.UserId!.Value,CommercialAssembler.ToCommands(resource.Items)),ct); return CreatedAtAction(nameof(GetById),new{id=r.Id},CommercialAssembler.From(r)); } catch(ArgumentException e){return Problem(statusCode:400,title:e.Message);} }
    [HttpGet][ProducesResponseType<IEnumerable<CommercialResponse>>(200)] public async Task<IActionResult> GetAll(CancellationToken ct)=>Ok((await queries.Handle(new GetSalesQuery(),ct)).Select(CommercialAssembler.From));
    [HttpGet("{id}")][ProducesResponseType<CommercialResponse>(200)][ProducesResponseType<ProblemDetails>(400)][ProducesResponseType<ProblemDetails>(404)]
    public async Task<IActionResult> GetById(Guid id,CancellationToken ct){try{var r=await queries.Handle(new GetSaleByIdQuery(id),ct);return r is null?Problem(statusCode:404,title:"No se encontró la venta solicitada."):Ok(CommercialAssembler.From(r));}catch(ArgumentException e){return Problem(statusCode:400,title:e.Message);}}
}
