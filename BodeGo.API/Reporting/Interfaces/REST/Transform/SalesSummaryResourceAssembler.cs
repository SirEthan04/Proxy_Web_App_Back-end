using BodeGo.API.Reporting.Application.Model.ReadModels;
using BodeGo.API.Reporting.Interfaces.REST.Resources;
namespace BodeGo.API.Reporting.Interfaces.REST.Transform;
public static class SalesSummaryResourceAssembler
{
    public static SalesSummaryResource From(SalesSummaryReadModel summary) => new(summary.SalesCount, summary.TotalAmount);
}
