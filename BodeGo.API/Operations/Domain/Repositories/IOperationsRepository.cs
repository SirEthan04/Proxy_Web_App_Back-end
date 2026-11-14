using BodeGo.API.Operations.Domain.Model.Aggregates;
using BodeGo.API.Operations.Domain.Model.Entities;
namespace BodeGo.API.Operations.Domain.Repositories;
public interface IOperationsRepository
{
    Task<IReadOnlyList<InventoryMovement>> ListMovementsAsync(CancellationToken ct);
    Task<IReadOnlyList<Waste>> ListWastesAsync(CancellationToken ct);
    Task<InventoryCount?> FindCountAsync(Guid id, CancellationToken ct);
    void Add(Waste waste, IReadOnlyList<InventoryMovement> movements);
    void Add(InventoryCount count);
}
