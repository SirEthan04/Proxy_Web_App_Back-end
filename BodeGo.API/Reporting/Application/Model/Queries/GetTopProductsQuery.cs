namespace BodeGo.API.Reporting.Application.Model.Queries;
public sealed record GetTopProductsQuery(DateOnly? From = null, DateOnly? To = null, int Limit = 10);
