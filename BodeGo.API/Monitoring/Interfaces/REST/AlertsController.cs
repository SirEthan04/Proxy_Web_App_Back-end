using BodeGo.API.Monitoring.Application.Services;
using BodeGo.API.Monitoring.Domain.Model.Queries;
using BodeGo.API.Monitoring.Interfaces.REST.Resources;
using BodeGo.API.Monitoring.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;
namespace BodeGo.API.Monitoring.Interfaces.REST;
[ApiController]
[Tags("Monitoring")]
[Route("api/v1/alerts")]
public class AlertsController(IAlertQueryService queries) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IEnumerable<AlertResource>>(200)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok((await queries.Handle(new GetAlertsQuery(), cancellationToken)).Select(AlertResourceAssembler.From));

    [HttpGet("{id}")]
    [ProducesResponseType<AlertResource>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(404)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var alertId) || alertId == Guid.Empty)
            return Problem(statusCode: 400, title: "El identificador de la alerta debe ser un GUID válido y no vacío.");
        var alert = await queries.Handle(new GetAlertByIdQuery(alertId), cancellationToken);
        return alert is null ? Problem(statusCode: 404, title: "No se encontró la alerta solicitada.")
            : Ok(AlertResourceAssembler.From(alert));
    }
}
