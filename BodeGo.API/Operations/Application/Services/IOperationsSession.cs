using BodeGo.API.Inventory.Domain.Model.Aggregates;
namespace BodeGo.API.Operations.Application.Services;
// Puerto explícito de coordinación con Inventory e IAM; nunca guarda por separado.
public interface IOperationsSession
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken ct);
    Task<Product?> LoadProductAsync(Guid id, CancellationToken ct);
    Task<bool> IsActiveUserAsync(Guid id, CancellationToken ct);
}
