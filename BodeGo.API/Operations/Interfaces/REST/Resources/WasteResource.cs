namespace BodeGo.API.Operations.Interfaces.REST.Resources;
public record WasteResource(Guid Id, Guid ProductId, Guid? LotId, Guid UserId,
    int Quantity, string Reason, DateTime RegisteredAt);
