namespace BodeGo.API.Operations.Domain.Model.Commands;
public record CountItemCommand(Guid ProductId, Guid? LotId, int CountedQuantity);
public record CreateInventoryCountCommand(Guid UserId, string? Notes, IReadOnlyList<CountItemCommand> Items);
