using BodeGo.API.Inventory.Application.Services;
using BodeGo.API.Inventory.Domain.Exceptions;
using BodeGo.API.Inventory.Domain.Model.Commands;
using BodeGo.API.Inventory.Interfaces.REST.Resources;
using BodeGo.API.Inventory.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;
namespace BodeGo.API.Inventory.Interfaces.REST;
[ApiController]
[Tags("Inventory")]
[Route("api/v1/categories")]
public class CategoriesController(ICategoryCommandService commands) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CategoryResource>(201)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(409)]
    public async Task<IActionResult> Create([FromBody] CreateCategoryResource resource, CancellationToken cancellationToken)
    {
        try
        {
            var category = await commands.Handle(new CreateCategoryCommand(resource.Name, resource.Description), cancellationToken);
            return Created($"/api/v1/categories/{category.Id}", CategoryResourceAssembler.From(category));
        }
        catch (DuplicateCategoryException exception) { return Problem(statusCode: 409, title: exception.Message); }
        catch (ArgumentException exception) { return Problem(statusCode: 400, title: exception.Message); }
    }
}
