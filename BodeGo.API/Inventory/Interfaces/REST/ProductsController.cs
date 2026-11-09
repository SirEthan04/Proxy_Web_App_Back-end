using BodeGo.API.Inventory.Application.Services;
using BodeGo.API.Inventory.Domain.Exceptions;
using BodeGo.API.Inventory.Domain.Model.Commands;
using BodeGo.API.Inventory.Interfaces.REST.Resources;
using BodeGo.API.Inventory.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;
namespace BodeGo.API.Inventory.Interfaces.REST;
[ApiController]
[Tags("Inventory")]
[Route("api/v1/products")]
public class ProductsController(IProductCommandService commands) : ControllerBase
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
            return Created($"/api/v1/products/{product.Id}", ProductResourceAssembler.From(product));
        }
        catch (CategoryNotFoundException exception) { return Problem(statusCode: 400, title: exception.Message); }
        catch (ArgumentException exception) { return Problem(statusCode: 400, title: exception.Message); }
    }
}
