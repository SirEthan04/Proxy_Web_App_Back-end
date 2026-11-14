using BodeGo.API.IAM.Domain.Model.Entities;
using BodeGo.API.Shared.Infrastructure.Persistence.EFC;
using Microsoft.EntityFrameworkCore;
namespace BodeGo.API.IAM.Infrastructure.Persistence.EFC;

public static class RoleInitializer
{
    public static async Task InitializeIamRolesAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BodeGoDbContext>();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var existing = await context.Roles.AsNoTracking().ToListAsync();
        var required = new[] { new Role(1, "Administrador"), new Role(2, "Empleado") };
        foreach (var role in required)
        {
            if (existing.Any(r => (r.Id == role.Id && r.Name != role.Name) ||
                (r.Id != role.Id && r.Name.Equals(role.Name, StringComparison.OrdinalIgnoreCase))))
                throw new InvalidOperationException("El catalogo de roles tiene identificadores o nombres diferentes. Revisarlo sin sobrescribir datos.");
        }
        foreach (var role in required.Where(role => existing.All(r => r.Id != role.Id)))
            context.Roles.Add(role);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}
