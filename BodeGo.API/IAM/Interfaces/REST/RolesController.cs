using BodeGo.API.IAM.Application.Services;
using BodeGo.API.IAM.Domain.Model.Queries;
using BodeGo.API.IAM.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;
namespace BodeGo.API.IAM.Interfaces.REST;

[ApiController]
[Route("api/v1/roles")]
public class RolesController(IRoleQueryService queries) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok((await queries.Handle(new GetRolesQuery(), cancellationToken)).Select(RoleResourceAssembler.From));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        if (id <= 0) return Problem(statusCode: 400, title: "El identificador debe ser positivo.");
        var role = await queries.Handle(new GetRoleByIdQuery(id), cancellationToken);
        return role is null ? NotFound() : Ok(RoleResourceAssembler.From(role));
    }
}
