using BodeGo.API.IAM.Application.Services;
using BodeGo.API.IAM.Domain.Exceptions;
using BodeGo.API.IAM.Domain.Model.Commands;
using BodeGo.API.IAM.Domain.Model.Queries;
using BodeGo.API.IAM.Interfaces.REST.Resources;
using BodeGo.API.IAM.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;
namespace BodeGo.API.IAM.Interfaces.REST;

[ApiController]
[Tags("IAM")]
[Route("api/v1/users")]
public class UsersController(IUserCommandService commands, IUserQueryService queries) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<UserResource>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateUserResource resource, CancellationToken cancellationToken)
    {
        try
        {
            var user = await commands.Handle(new CreateUserCommand(resource.Name, resource.Email,
                resource.Password, resource.RoleId), cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, UserResourceAssembler.From(user));
        }
        catch (DuplicateEmailException exception)
        {
            return Problem(statusCode: 409, title: exception.Message);
        }
        catch (RoleNotFoundException exception)
        {
            return Problem(statusCode: 400, title: exception.Message);
        }
        catch (ArgumentException exception)
        {
            return Problem(statusCode: 400, title: exception.Message);
        }
    }

    [HttpGet]
    [ProducesResponseType<IEnumerable<UserResource>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok((await queries.Handle(new GetUsersQuery(), cancellationToken)).Select(UserResourceAssembler.From));

    [HttpGet("{id}")]
    [ProducesResponseType<UserResource>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty) return Problem(statusCode: 400, title: "El identificador no puede estar vacío.");
        var user = await queries.Handle(new GetUserByIdQuery(id), cancellationToken);
        return user is null ? Problem(statusCode: 404, title: "No se encontró el usuario solicitado.") : Ok(UserResourceAssembler.From(user));
    }
}
