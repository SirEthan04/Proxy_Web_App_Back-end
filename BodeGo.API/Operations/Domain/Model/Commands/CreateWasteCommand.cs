namespace BodeGo.API.Operations.Domain.Model.Commands;
public record CreateWasteCommand(Guid ProductId, Guid? LotId, Guid UserId, int Quantity, string Reason);
