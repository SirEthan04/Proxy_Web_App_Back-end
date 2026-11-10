namespace BodeGo.API.Reporting.Interfaces.REST.Resources;
public sealed record TopProductResource(Guid ProductId, string ProductName, long QuantitySold);
