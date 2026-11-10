using BodeGo.API.Operations.Application.Services;
using BodeGo.API.Operations.Domain;
using BodeGo.API.Operations.Domain.Exceptions;
using BodeGo.API.Operations.Domain.Model.Aggregates;
using BodeGo.API.Operations.Domain.Model.Commands;
using BodeGo.API.Operations.Domain.Repositories;
using BodeGo.API.Operations.Domain.Services;
namespace BodeGo.API.Operations.Application.Internal.CommandServices;
public class WasteCommandService(IOperationsRepository repository, IOperationsSession session) : IWasteCommandService
{
    public Task<Waste> Handle(CreateWasteCommand command, CancellationToken ct = default)
    {
        OperationsRules.Id(command.ProductId);
        OperationsRules.Id(command.UserId);
        OperationsRules.Lot(command.LotId);
        OperationsRules.Text(command.Reason, 255, "motivo");
        if (command.Quantity <= 0) throw new OperationsException("La cantidad de merma debe ser positiva.");
        return session.ExecuteAsync(async () =>
        {
            if (!await session.IsActiveUserAsync(command.UserId, ct))
                throw new OperationsException("El usuario no existe o está inactivo.");
            var product = await session.LoadProductAsync(command.ProductId, ct)
                ?? throw new OperationsException("El producto no existe.");
            if (!product.Active) throw new OperationsException("El producto está inactivo.");
            var waste = new Waste(command.ProductId, command.LotId, command.UserId, command.Quantity, command.Reason);
            var movements = WasteStockPolicy.Apply(product, waste);
            repository.Add(waste, movements);
            return waste;
        }, ct);
    }
}
