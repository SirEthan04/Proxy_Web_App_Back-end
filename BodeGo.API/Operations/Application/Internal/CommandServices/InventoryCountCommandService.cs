using BodeGo.API.Operations.Application.Services;
using BodeGo.API.Operations.Domain;
using BodeGo.API.Operations.Domain.Exceptions;
using BodeGo.API.Operations.Domain.Model.Aggregates;
using BodeGo.API.Operations.Domain.Model.Commands;
using BodeGo.API.Operations.Domain.Repositories;
using BodeGo.API.IAM.Application.Services;
namespace BodeGo.API.Operations.Application.Internal.CommandServices;
public class InventoryCountCommandService(IOperationsRepository repository, IOperationsSession session, ICurrentUser currentUser) : IInventoryCountCommandService
{
    public Task<InventoryCount> Handle(CreateInventoryCountCommand command, CancellationToken ct = default)
    {
        OperationsIdentity.RequireActor(currentUser.User, command.UserId);
        OperationsRules.Id(command.UserId);
        OperationsRules.Text(command.Notes, 500, "notas", optional: true);
        if (command.Items == null || command.Items.Count == 0)
            throw new OperationsException("El conteo debe incluir al menos un detalle.");
        foreach (var item in command.Items)
        {
            if (item == null) throw new OperationsException("Los detalles no pueden ser nulos.");
            OperationsRules.Id(item.ProductId);
            OperationsRules.Lot(item.LotId);
            if (item.CountedQuantity < 0) throw new OperationsException("La cantidad física no puede ser negativa.");
        }
        foreach (var group in command.Items.GroupBy(x => x.ProductId))
        {
            if (group.GroupBy(x => x.LotId).Any(x => x.Count() > 1))
                throw new OperationsException("No repita el mismo producto y lote.");
            if (group.Count() > 1 && group.Any(x => x.LotId == null))
                throw new OperationsException("No mezcle el conteo total de un producto con sus lotes.");
        }
        return session.ExecuteAsync(async () =>
        {
            if (!await session.IsActiveUserAsync(command.UserId, ct))
                throw new OperationsException("El usuario no existe o está inactivo.");
            var count = new InventoryCount(command.UserId, command.Notes);
            foreach (var item in command.Items)
            {
                var product = await session.LoadProductAsync(item.ProductId, ct)
                    ?? throw new OperationsException("El producto no existe.");
                var expected = product.Stock.Value;
                if (item.LotId.HasValue)
                {
                    var lot = product.Lots.SingleOrDefault(x => x.Id == item.LotId)
                        ?? throw new OperationsException("El lote no existe o no corresponde al producto.");
                    expected = lot.QuantityAvailable;
                }
                count.AddItem(item.ProductId, item.LotId, expected, item.CountedQuantity);
            }
            // Se registra una fotografía confirmada; no se ajusta Product ni se crean movimientos.
            count.Confirm();
            repository.Add(count);
            return count;
        }, ct);
    }
}
