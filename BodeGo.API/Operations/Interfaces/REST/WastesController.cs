using BodeGo.API.Operations.Application.Services;
using BodeGo.API.Operations.Domain.Exceptions;
using BodeGo.API.Operations.Interfaces.REST.Resources;
using BodeGo.API.Operations.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BodeGo.API.IAM.Application.Services;
using BodeGo.API.IAM.Infrastructure.Authentication;
namespace BodeGo.API.Operations.Interfaces.REST;
[ApiController]
[Tags("Operations")]
[Route("api/v1/wastes")]
public class WastesController(IWasteCommandService commands, IWasteQueryService queries, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IEnumerable<WasteResource>>(200)]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok((await queries.Handle(new BodeGo.API.Operations.Domain.Model.Queries.GetWastesQuery(), ct))
            .Select(WasteResourceAssembler.From));

    [HttpPost]
    [Authorize(Policy = IamAuthenticationExtensions.Operations)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<WasteResource>(201)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(409)]
    public async Task<IActionResult> Create(CreateWasteResource resource, CancellationToken ct)
    {
        try
        {
            var waste = await commands.Handle(WasteResourceAssembler.ToCommand(resource, currentUser.User.Id), ct);
            return Created("/api/v1/wastes", WasteResourceAssembler.From(waste));
        }
        catch (OperationsException e) { return Problem(statusCode: e.Forbidden ? 403 : e.Conflict ? 409 : 400, title: e.Message); }
    }
}
