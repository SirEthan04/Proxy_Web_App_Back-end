using BodeGo.API.IAM.Domain.Model.Aggregates;
using BodeGo.API.IAM.Domain.Model.Entities;
using BodeGo.API.IAM.Domain.Model.ValueObjects;
using BodeGo.API.Inventory.Domain.Model.Aggregates;
using BodeGo.API.Inventory.Domain.Model.Entities;
using BodeGo.API.Inventory.Domain.Model.ValueObjects;
using BodeGo.API.Operations.Application.Internal.CommandServices;
using BodeGo.API.Operations.Application.Services;
using BodeGo.API.Operations.Domain.Exceptions;
using BodeGo.API.Operations.Domain.Model.Commands;
using BodeGo.API.Operations.Infrastructure.Persistence.EFC;
using BodeGo.API.Operations.Infrastructure.Persistence.EFC.Repositories;
using BodeGo.API.Shared.Infrastructure.Persistence.EFC;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MySql.Data.MySqlClient;
using BodeGo.API.IAM.Infrastructure.Hashing;

// Integración optativa: requiere una base desechable con la migración existente aplicada.
// No crea esquemas ni borra historia. Los datos de prueba permanecen identificados.
internal static class MySqlChecks
{
    public static async Task Run()
    {
        var connection = Environment.GetEnvironmentVariable("BODEGO_OPERATIONS_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Configure BODEGO_OPERATIONS_TEST_CONNECTION para --mysql.");
        var settings = new MySqlConnectionStringBuilder(connection);
        if (settings.Server is not ("localhost" or "127.0.0.1") ||
            !(settings.Database.StartsWith("bodego_operations_test", StringComparison.OrdinalIgnoreCase)
                || settings.Database.StartsWith("bodego_test_autenticacion_", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Se requiere una base local aislada de Operations o autenticación.");
        DbContextOptions<BodeGoDbContext> Options(bool fail = false)
        {
            var builder = new DbContextOptionsBuilder<BodeGoDbContext>().UseMySQL(connection);
            if (fail) builder.AddInterceptors(new FailAfterSave());
            return builder.Options;
        }
        var suffix = Guid.NewGuid().ToString("N");
        var category = new Category("Operations test " + suffix);
        var product = new Product(category.Id, new ProductName("Prueba Operations"), new Money(1), new StockLevel(0));
        var lot = product.ReceiveLot("TEST", null, 10);
        User user;
        await using (var seed = new BodeGoDbContext(Options()))
        {
            var role = await seed.Roles.SingleOrDefaultAsync(r => r.Name == "Empleado");
            if (role == null) { role = new Role(2, "Empleado"); seed.Roles.Add(role); }
            var password = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24));
            user = new User(role.Id, "Operations test", new EmailAddress(suffix + "@example.test"), new PasswordHashService().Hash(password));
            seed.Users.Add(user); seed.Categories.Add(category); seed.Products.Add(product);
            await seed.SaveChangesAsync();
        }
        CreateWasteCommand command = new(product.Id, lot.Id, user.Id, 6, "Prueba de atomicidad");
        var actor = new OperationsTestIdentity(user.Id);
        await using (var failing = new BodeGoDbContext(Options(fail: true)))
        {
            try
            {
                await new WasteCommandService(new OperationsRepository(failing), new OperationsSession(failing), actor).Handle(command);
                throw new Exception("La falla inyectada no ocurrió.");
            }
            catch (InjectedFailure) { }
        }
        await using (var verify = new BodeGoDbContext(Options()))
        {
            if ((await verify.Products.SingleAsync(x => x.Id == product.Id)).Stock.Value != 10 ||
                (await verify.ProductLots.SingleAsync(x => x.Id == lot.Id)).QuantityAvailable != 10 ||
                await verify.Wastes.AnyAsync(x => x.ProductId == product.Id) ||
                await verify.InventoryMovements.AnyAsync(x => x.ProductId == product.Id))
                throw new Exception("Falló rollback después de SaveChanges.");
        }
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var loaded = 0;
        async Task<bool> Compete()
        {
            await using var db = new BodeGoDbContext(Options());
            var session = new BarrierSession(new OperationsSession(db), async () =>
            {
                if (Interlocked.Increment(ref loaded) == 2) ready.SetResult();
                await ready.Task.WaitAsync(TimeSpan.FromSeconds(20));
            });
            try { await new WasteCommandService(new OperationsRepository(db), session, actor).Handle(command); return true; }
            catch (OperationsException e) when (e.Conflict) { return false; }
        }
        var results = await Task.WhenAll(Compete(), Compete());
        if (results.Count(x => x) != 1) throw new Exception("La concurrencia debe aceptar una sola merma.");
        await using (var verify = new BodeGoDbContext(Options()))
        {
            var saved = await verify.Wastes.SingleAsync(x => x.ProductId == product.Id);
            var movements = await verify.InventoryMovements.Where(x => x.ProductId == product.Id).ToArrayAsync();
            if ((await verify.Products.SingleAsync(x => x.Id == product.Id)).Stock.Value != 4 ||
                (await verify.ProductLots.SingleAsync(x => x.Id == lot.Id)).QuantityAvailable != 4 ||
                movements.Sum(x => x.QuantityDelta) != -6 || movements.Any(x => x.SourceId != saved.Id.ToString()))
                throw new Exception("Stock o movimientos incoherentes después de concurrencia.");
            var before = await verify.InventoryMovements.CountAsync(x => x.ProductId == product.Id);
            var count = await new InventoryCountCommandService(new OperationsRepository(verify), new OperationsSession(verify), actor)
                .Handle(new CreateInventoryCountCommand(user.Id, "Prueba MySQL", [new(product.Id, lot.Id, 3)]));
            verify.ChangeTracker.Clear();
            var read = await new OperationsRepository(verify).FindCountAsync(count.Id, default);
            if (read?.Items.Single().Difference != -1 ||
                (await verify.Products.SingleAsync(x => x.Id == product.Id)).Stock.Value != 4 ||
                await verify.InventoryMovements.CountAsync(x => x.ProductId == product.Id) != before)
                throw new Exception("El conteo alteró stock o no conservó sus detalles.");
        }
        Console.WriteLine($"OK MySQL: rollback tras SaveChanges, concurrencia, stock, movimientos y conteo. Producto de prueba: {product.Id}");
    }
    private sealed class InjectedFailure : Exception;
    private sealed class FailAfterSave : SaveChangesInterceptor
    {
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
            CancellationToken cancellationToken = default) => throw new InjectedFailure();
    }
    private sealed class BarrierSession(IOperationsSession inner, Func<Task> loaded) : IOperationsSession
    {
        public Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken ct) => inner.ExecuteAsync(operation, ct);
        public Task<bool> IsActiveUserAsync(Guid id, CancellationToken ct) => inner.IsActiveUserAsync(id, ct);
        public async Task<Product?> LoadProductAsync(Guid id, CancellationToken ct)
        {
            var product = await inner.LoadProductAsync(id, ct);
            await loaded();
            return product;
        }
    }
}
