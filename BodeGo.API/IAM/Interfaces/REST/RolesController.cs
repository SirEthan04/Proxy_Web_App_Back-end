using BodeGo.API.IAM.Application.Services;
using BodeGo.API.IAM.Domain.Model.Queries;
using BodeGo.API.IAM.Interfaces.REST.Transform;
using BodeGo.API.IAM.Interfaces.REST.Resources;
using Microsoft.AspNetCore.Mvc;
namespace BodeGo.API.IAM.Interfaces.REST;

[ApiController]
[Tags("IAM")]
[Route("api/v1/roles")]
public class RolesController(IRoleQueryService queries) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IEnumerable<RoleResource>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok((await queries.Handle(new GetRolesQuery(), cancellationToken)).Select(RoleResourceAssembler.From));

    [HttpGet("{id}")]
    [ProducesResponseType<RoleResource>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        if (id <= 0) return Problem(statusCode: 400, title: "El identificador debe ser positivo.");
        var role = await queries.Handle(new GetRoleByIdQuery(id), cancellationToken);
        return role is null ? Problem(statusCode: 404, title: "No se encontró el rol solicitado.") : Ok(RoleResourceAssembler.From(role));
    }
}
