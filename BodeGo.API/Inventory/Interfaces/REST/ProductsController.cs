using BodeGo.API.Inventory.Application.Services;
using BodeGo.API.Inventory.Domain.Exceptions;
using BodeGo.API.Inventory.Domain.Model.Commands;
using BodeGo.API.Inventory.Domain.Model.Queries;
using BodeGo.API.Inventory.Interfaces.REST.Resources;
using BodeGo.API.Inventory.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;
namespace BodeGo.API.Inventory.Interfaces.REST;
[ApiController]
[Tags("Inventory")]
[Route("api/v1/products")]
public class ProductsController(IProductCommandService commands, IProductQueryService queries) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ProductResource>(201)]
    [ProducesResponseType<ProblemDetails>(400)]
    public async Task<IActionResult> Create([FromBody] CreateProductResource resource, CancellationToken cancellationToken)
    {
        try
        {
            var product = await commands.Handle(new CreateProductCommand(resource.CategoryId!.Value, resource.Name,
                resource.SalePrice!.Value, resource.MinimumStock!.Value), cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, ProductResourceAssembler.From(product));
        }
        catch (CategoryNotFoundException exception) { return Problem(statusCode: 400, title: exception.Message); }
        catch (ArgumentException exception) { return Problem(statusCode: 400, title: exception.Message); }
    }

    [HttpGet]
    [ProducesResponseType<IEnumerable<ProductResource>>(200)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok((await queries.Handle(new GetProductsQuery(), cancellationToken)).Select(ProductResourceAssembler.From));

    [HttpGet("{id}")]
    [ProducesResponseType<ProductResource>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty) return Problem(statusCode: 400, title: "El identificador no puede estar vacío.");
        var product = await queries.Handle(new GetProductByIdQuery(id), cancellationToken);
        return product is null ? Problem(statusCode: 404, title: "No se encontró el producto solicitado.") : Ok(ProductResourceAssembler.From(product));
    }

    [HttpGet("{id}/lots")]
    [ProducesResponseType<IEnumerable<ProductLotResource>>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(404)]
    public async Task<IActionResult> GetLots(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty) return Problem(statusCode: 400, title: "El identificador no puede estar vacío.");
        var lots = await queries.Handle(new GetProductLotsQuery(id), cancellationToken);
        return lots is null ? Problem(statusCode: 404, title: "No se encontró el producto solicitado.") : Ok(lots.Select(ProductLotResourceAssembler.From));
    }

    // Literal routes take precedence over the parameter route.
    [HttpGet("low-stock")]
    [ProducesResponseType<IEnumerable<ProductResource>>(200)]
    public async Task<IActionResult> GetLowStock(CancellationToken cancellationToken) =>
        Ok((await queries.Handle(new GetLowStockProductsQuery(), cancellationToken)).Select(ProductResourceAssembler.From));
}
