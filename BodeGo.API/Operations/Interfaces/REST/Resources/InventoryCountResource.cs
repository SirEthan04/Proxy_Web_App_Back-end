namespace BodeGo.API.Operations.Interfaces.REST.Resources;
public record InventoryCountItemResource(Guid Id, Guid ProductId, Guid? LotId,
    int ExpectedQuantity, int CountedQuantity, int Difference);
public record InventoryCountResource(Guid Id, Guid UserId, DateTime CountedAt, string Status,
    string? Notes, IReadOnlyList<InventoryCountItemResource> Items);
