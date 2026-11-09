using BodeGo.API.IAM.Application.Services;
using BodeGo.API.IAM.Domain.Exceptions;
using BodeGo.API.IAM.Domain.Model.Commands;
using BodeGo.API.IAM.Domain.Model.Queries;
using BodeGo.API.IAM.Interfaces.REST.Resources;
using BodeGo.API.IAM.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;
namespace BodeGo.API.IAM.Interfaces.REST;

[ApiController]
[Route("api/v1/users")]
public class UsersController(IUserCommandService commands, IUserQueryService queries) : ControllerBase
{
    [HttpPost]
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
        catch (ArgumentException)
        {
            return Problem(statusCode: 400, title: "Nombre, correo o contraseña invalidos.");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok((await queries.Handle(new GetUsersQuery(), cancellationToken)).Select(UserResourceAssembler.From));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty) return Problem(statusCode: 400, title: "El identificador no puede estar vacio.");
        var user = await queries.Handle(new GetUserByIdQuery(id), cancellationToken);
        return user is null ? NotFound() : Ok(UserResourceAssembler.From(user));
    }
}
