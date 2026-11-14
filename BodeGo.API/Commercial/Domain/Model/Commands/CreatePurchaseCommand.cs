namespace BodeGo.API.Commercial.Domain.Model.Commands;
public sealed record CommercialItemCommand(Guid ProductId, Guid? LotId, int Quantity, decimal UnitAmount);
public sealed record CreatePurchaseCommand(Guid UserId, IReadOnlyList<CommercialItemCommand> Items);
