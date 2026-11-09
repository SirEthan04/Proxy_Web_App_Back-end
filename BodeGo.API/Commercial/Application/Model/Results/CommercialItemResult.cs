namespace BodeGo.API.Commercial.Application.Model.Results;
public sealed record CommercialItemResult(Guid Id, Guid ProductId, Guid? LotId, int Quantity, decimal UnitAmount, decimal Subtotal);
