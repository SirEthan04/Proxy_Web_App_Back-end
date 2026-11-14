using System.Data;
using BodeGo.API.Commercial.Application.Services;
using BodeGo.API.Commercial.Domain.Exceptions;
using BodeGo.API.Commercial.Domain.Model.Aggregates;
using BodeGo.API.Inventory.Domain.Model.Aggregates;
using BodeGo.API.Operations.Domain.Model.Entities;
using BodeGo.API.Shared.Infrastructure.Persistence.EFC;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
namespace BodeGo.API.Commercial.Infrastructure.Persistence.EFC;

public class CommercialConfirmationSession(BodeGoDbContext context) : ICommercialConfirmationSession
{
    public async Task<Purchase?> LockPurchaseAsync(Guid id, CancellationToken ct)
    {
        RequireTransaction();
        var rows = await context.Purchases.FromSqlInterpolated(
            $"SELECT * FROM purchases WHERE id = {id.ToString()} FOR UPDATE").AsTracking().ToListAsync(ct);
        var purchase = rows.SingleOrDefault();
        if (purchase != null) await context.Entry(purchase).Collection(x => x.Items).LoadAsync(ct);
        return purchase;
    }
    public async Task<Sale?> LockSaleAsync(Guid id, CancellationToken ct)
    {
        RequireTransaction();
        var rows = await context.Sales.FromSqlInterpolated(
            $"SELECT * FROM sales WHERE id = {id.ToString()} FOR UPDATE").AsTracking().ToListAsync(ct);
        var sale = rows.SingleOrDefault();
        if (sale != null) await context.Entry(sale).Collection(x => x.Items).LoadAsync(ct);
        return sale;
    }
    public async Task<Product?> LockProductAsync(Guid id, CancellationToken ct)
    {
        RequireTransaction();
        var rows = await context.Products.FromSqlInterpolated(
            $"SELECT * FROM products WHERE id = {id.ToString()} FOR UPDATE").AsTracking().ToListAsync(ct);
        var product = rows.SingleOrDefault();
        if (product != null) await context.Entry(product).Collection(x => x.Lots).LoadAsync(ct);
        return product;
    }
    public Task<bool> IsActiveUserAsync(Guid id, CancellationToken ct) =>
        context.Users.AsNoTracking().AnyAsync(u => u.Id == id && u.Active, ct);
    public void AddMovements(IEnumerable<InventoryMovement> movements) => context.InventoryMovements.AddRange(movements);

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken ct)
    {
        if (context.ChangeTracker.HasChanges() || context.Database.CurrentTransaction != null)
            throw new InvalidOperationException("La confirmación requiere una unidad de trabajo sin cambios pendientes.");
        // Una lectura bloqueante debe devolver el estado actual, no una instancia rastreada antigua.
        context.ChangeTracker.Clear();
        try
        {
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            var result = await operation();
            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return result;
        }
        catch (DbUpdateConcurrencyException)
        {
            context.ChangeTracker.Clear();
            throw new CommercialConfirmationException("El inventario cambió durante la confirmación. Consulte la operación y reintente.", ConfirmationFailure.Conflict);
        }
        catch (Exception exception) when (IsConflict(exception))
        {
            context.ChangeTracker.Clear();
            throw new CommercialConfirmationException("Conflicto de concurrencia o persistencia. Consulte la operación y reintente.", ConfirmationFailure.Conflict);
        }
        catch (Exception exception) when (exception is MySqlException || exception.InnerException is MySqlException)
        {
            context.ChangeTracker.Clear();
            throw new CommercialConfirmationException("No se pudo completar la confirmación. Consulte su estado antes de reintentar.", ConfirmationFailure.Unavailable);
        }
        catch (OverflowException)
        {
            context.ChangeTracker.Clear();
            throw new CommercialConfirmationException("La cantidad supera la capacidad del inventario.", ConfirmationFailure.Conflict);
        }
        catch
        {
            context.ChangeTracker.Clear();
            throw;
        }
        // DisposeAsync revierte cualquier fallo previo al commit, incluso después de SaveChanges.
    }
    private void RequireTransaction()
    {
        if (context.Database.CurrentTransaction == null)
            throw new InvalidOperationException("Los bloqueos requieren una transacción activa.");
    }
    private static bool IsConflict(Exception exception) =>
        exception is MySqlException { Number: 1213 or 1205 or 1452 or 1062 } ||
        exception.InnerException is MySqlException { Number: 1213 or 1205 or 1452 or 1062 };
}
