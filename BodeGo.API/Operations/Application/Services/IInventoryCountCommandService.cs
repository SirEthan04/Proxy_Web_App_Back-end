using BodeGo.API.Operations.Domain.Model.Aggregates;
using BodeGo.API.Operations.Domain.Model.Commands;
namespace BodeGo.API.Operations.Application.Services;
public interface IInventoryCountCommandService
{
    Task<InventoryCount> Handle(CreateInventoryCountCommand command, CancellationToken ct = default);
}
