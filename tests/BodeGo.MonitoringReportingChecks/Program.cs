using BodeGo.API.Monitoring.Application.Internal.QueryServices;
using BodeGo.API.Monitoring.Domain.Model.Aggregates;
using BodeGo.API.Monitoring.Domain.Model.Queries;
using BodeGo.API.Monitoring.Domain.Model.ValueObjects;
using BodeGo.API.Monitoring.Domain.Repositories;
using BodeGo.API.Monitoring.Interfaces.REST;
using BodeGo.API.Reporting.Application.Internal.QueryServices;
using BodeGo.API.Reporting.Application.Model.Queries;
using BodeGo.API.Reporting.Application.Model.ReadModels;
using BodeGo.API.Reporting.Application.Services;
using BodeGo.API.Reporting.Interfaces.REST;
using BodeGo.API.Reporting.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;

var checks = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
    checks++;
}
void Reject(Action action)
{
    try { action(); }
    catch (ArgumentException) { checks++; return; }
    throw new InvalidOperationException("La entrada inválida fue aceptada.");
}

var alerts = new AlertMemoryRepository();
var service = new AlertQueryService(alerts);
Check((await service.Handle(new GetAlertsQuery())).Count == 0, "La lista vacía debe conservarse.");
var alert = new Alert(Guid.NewGuid(), null, AlertType.LowStock, "Alerta de prueba unitaria, no persistida");
alerts.Values.Add(alert);
Check((await service.Handle(new GetAlertsQuery())).Single().Id == alert.Id, "La alerta no se recuperó.");
var result = await service.Handle(new GetAlertByIdQuery(alert.Id));
Check(result?.AlertType == "LowStock" && result.Status == "Pending" && result.CreatedAt.Kind == DateTimeKind.Utc, "Contrato de alerta incorrecto.");
Check(await service.Handle(new GetAlertByIdQuery(Guid.NewGuid())) is null, "Alerta ausente debe devolver null.");
Reject(() => service.Handle(new GetAlertByIdQuery(Guid.Empty)).GetAwaiter().GetResult());
var controller = new AlertsController(service);
Check(await controller.GetById("invalid", default) is ObjectResult { StatusCode: 400 }, "GUID inválido debe devolver 400.");
Check(await controller.GetById(Guid.Empty.ToString(), default) is ObjectResult { StatusCode: 400 }, "GUID vacío debe devolver 400.");
var notFound = (ObjectResult)await controller.GetById(Guid.NewGuid().ToString(), default);
Check(notFound.StatusCode == 404 && ((ProblemDetails)notFound.Value!).Title == "No se encontró la alerta solicitada.", "404 sin mensaje español.");
Check(await controller.GetById(alert.Id.ToString(), default) is OkObjectResult, "Alerta existente debe devolver 200.");
Check(alert.Status == AlertStatus.Pending && alert.AttendedAt is null, "La consulta modificó la alerta.");

var day = new DateOnly(2026, 11, 10);
var range = new ReportDateRange(day, day);
Check(range.StartUtc == new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc), "Inicio de día incorrecto.");
Check(range.EndExclusiveUtc == range.StartUtc.GetValueOrDefault().AddDays(1), "El día final no es inclusivo.");
Check(new ReportDateRange(null, null).StartUtc is null, "El filtro omitido no debe limitar fechas.");
Check(new ReportDateRange(day, DateOnly.MaxValue).EndExclusiveUtc is null, "La fecha máxima produjo overflow.");
Reject(() => new ReportDateRange(day, day.AddDays(-1)));
Reject(() => new ReportDateRange(new DateOnly(999, 1, 1), null));
Reject(() => ReportParameterParser.Date("10/11/2026"));
Reject(() => ReportParameterParser.Date("2026-02-30"));
Check(ReportParameterParser.Date("2026-11-10") == day, "Formato ISO no reconocido.");
Reject(() => ReportParameterParser.Limit("invalid"));
Reject(() => ReportParameterParser.Limit(null));

var repositories = new ReportMemoryRepositories();
var summaries = new SalesSummaryQueryService(repositories);
var products = new TopProductsQueryService(repositories);
Check((await summaries.Handle(new GetSalesSummaryQuery())).TotalAmount == 0, "Resumen vacío incorrecto.");
Check((await products.Handle(new GetTopProductsQuery())).Count == 0, "Ranking vacío incorrecto.");
await products.Handle(new GetTopProductsQuery(day, day, 1));
Check(repositories.Limit == 1 && repositories.Range?.EndExclusiveUtc == range.EndExclusiveUtc, "Filtros no enviados al repositorio.");
Reject(() => products.Handle(new GetTopProductsQuery(Limit: 0)).GetAwaiter().GetResult());
Reject(() => products.Handle(new GetTopProductsQuery(Limit: 101)).GetAwaiter().GetResult());
Reject(() => summaries.Handle(new GetSalesSummaryQuery(day, day.AddDays(-1))).GetAwaiter().GetResult());
var reports = new ReportsController(summaries, products);
Check(await reports.TopProducts(null, null, default, "abc") is ObjectResult { StatusCode: 400 }, "Límite mal formado debe devolver 400.");
Check(await reports.SalesSummary("2026-11-11", "2026-11-10", default) is ObjectResult { StatusCode: 400 }, "Rango inválido debe devolver 400.");
Console.WriteLine($"OK: {checks} comprobaciones unitarias; dobles explícitos, sin acceso a MySQL.");
if (args is ["--integration", var baseUrl]) await IntegrationChecks.Run(baseUrl);
else if (args.Length != 0) throw new ArgumentException("Uso: --integration <URL local de la API>");

sealed class AlertMemoryRepository : IAlertRepository
{
    public List<Alert> Values { get; } = [];
    public Task<IReadOnlyList<Alert>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Alert>>(Values.ToArray());
    public Task<Alert?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Values.SingleOrDefault(a => a.Id == id));
}
sealed class ReportMemoryRepositories : ISalesSummaryRepository, ITopProductsRepository
{
    public ReportDateRange? Range { get; private set; }
    public int Limit { get; private set; }
    public Task<SalesSummaryReadModel> GetAsync(ReportDateRange range, CancellationToken cancellationToken = default) => Task.FromResult(new SalesSummaryReadModel(0, 0));
    public Task<IReadOnlyList<TopProductReadModel>> GetAsync(ReportDateRange range, int limit, CancellationToken cancellationToken = default)
    {
        Range = range; Limit = limit;
        return Task.FromResult<IReadOnlyList<TopProductReadModel>>([]);
    }
}
