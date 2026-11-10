using System.Data;
using BodeGo.API.Inventory.Domain.Model.Aggregates;
using BodeGo.API.Operations.Application.Services;
using BodeGo.API.Operations.Domain.Exceptions;
using BodeGo.API.Shared.Infrastructure.Persistence.EFC;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
namespace BodeGo.API.Operations.Infrastructure.Persistence.EFC;
public class OperationsSession(BodeGoDbContext context) : IOperationsSession
{
    public Task<Product?> LoadProductAsync(Guid id, CancellationToken ct) =>
        context.Products.Include(x => x.Lots).SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<bool> IsActiveUserAsync(Guid id, CancellationToken ct) =>
        context.Users.AsNoTracking().AnyAsync(x => x.Id == id && x.Active, ct);
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken ct)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        try
        {
            var result = await operation();
            // Una sola unidad de trabajo: producto, lotes, merma y movimientos.
            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return result;
        }
        catch (DbUpdateConcurrencyException)
        {
            context.ChangeTracker.Clear();
            throw new OperationsException("El stock cambió durante la operación. Consulte las existencias y reintente.", true);
        }
        catch (Exception exception) when (IsConflict(exception))
        {
            context.ChangeTracker.Clear();
            throw new OperationsException("Conflicto de persistencia o concurrencia. Revise las referencias y reintente.", true);
        }
        catch
        {
            context.ChangeTracker.Clear();
            throw;
        }
        // DisposeAsync revierte la transacción ante cualquier fallo, incluso después de SaveChanges.
    }
    private static bool IsConflict(Exception exception) =>
        exception is MySqlException { Number: 1213 or 1205 or 1452 or 1062 } ||
        exception.InnerException is MySqlException { Number: 1213 or 1205 or 1452 or 1062 };
}
