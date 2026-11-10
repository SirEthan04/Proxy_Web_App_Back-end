using BodeGo.API.Reporting.Application.Model.Queries;
using BodeGo.API.Reporting.Application.Services;
using BodeGo.API.Reporting.Interfaces.REST.Resources;
using BodeGo.API.Reporting.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;
namespace BodeGo.API.Reporting.Interfaces.REST;
[ApiController]
[Tags("Reporting")]
[Route("api/v1/reports")]
public class ReportsController(ISalesSummaryQueryService summaries) : ControllerBase
{
    [HttpGet("sales-summary")]
    [ProducesResponseType<SalesSummaryResource>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    public async Task<IActionResult> SalesSummary([FromQuery] string? from, [FromQuery] string? to, CancellationToken cancellationToken)
    {
        try
        {
            var summary = await summaries.Handle(new GetSalesSummaryQuery(ReportParameterParser.Date(from),
                ReportParameterParser.Date(to)), cancellationToken);
            return Ok(SalesSummaryResourceAssembler.From(summary));
        }
        catch (ArgumentException exception) { return Problem(statusCode: 400, title: exception.Message); }
    }
}
