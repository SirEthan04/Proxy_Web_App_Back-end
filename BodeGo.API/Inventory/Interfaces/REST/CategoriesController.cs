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
[Route("api/v1/categories")]
public class CategoriesController(ICategoryCommandService commands, ICategoryQueryService queries) : ControllerBase
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
            return CreatedAtAction(nameof(GetById), new { id = category.Id }, CategoryResourceAssembler.From(category));
        }
        catch (DuplicateCategoryException exception) { return Problem(statusCode: 409, title: exception.Message); }
        catch (ArgumentException exception) { return Problem(statusCode: 400, title: exception.Message); }
    }

    [HttpGet]
    [ProducesResponseType<IEnumerable<CategoryResource>>(200)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok((await queries.Handle(new GetCategoriesQuery(), cancellationToken)).Select(CategoryResourceAssembler.From));

    [HttpGet("{id}")]
    [ProducesResponseType<CategoryResource>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty) return Problem(statusCode: 400, title: "El identificador no puede estar vacío.");
        var category = await queries.Handle(new GetCategoryByIdQuery(id), cancellationToken);
        return category is null ? Problem(statusCode: 404, title: "No se encontró la categoría solicitada.") : Ok(CategoryResourceAssembler.From(category));
    }
}
