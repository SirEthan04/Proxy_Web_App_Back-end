namespace BodeGo.API.Reporting.Application.Model.Queries;
public sealed record GetSalesSummaryQuery(DateOnly? From = null, DateOnly? To = null);
