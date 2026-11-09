namespace BodeGo.API.Commercial.Domain.Model.Commands;
public sealed record CreateSaleCommand(Guid UserId, IReadOnlyList<CommercialItemCommand> Items);
