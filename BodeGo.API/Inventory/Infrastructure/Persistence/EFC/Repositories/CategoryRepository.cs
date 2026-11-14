using BodeGo.API.Inventory.Domain.Exceptions;
using BodeGo.API.Inventory.Domain.Model.Entities;
using BodeGo.API.Inventory.Domain.Repositories;
using BodeGo.API.Shared.Infrastructure.Persistence.EFC;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
namespace BodeGo.API.Inventory.Infrastructure.Persistence.EFC.Repositories;
public class CategoryRepository(BodeGoDbContext context) : ICategoryRepository
{
    public Task<Category?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Categories.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
    public async Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default) =>
        await context.Categories.AsNoTracking().OrderBy(c => c.Name).ThenBy(c => c.Id).ToListAsync(cancellationToken);
    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default) =>
        context.Categories.AnyAsync(c => c.Name == name, cancellationToken);
    public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        context.Categories.Add(category);
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (exception.InnerException is MySqlException { Number: 1062 } sql &&
            sql.Message.Contains("IX_categories_name", StringComparison.OrdinalIgnoreCase))
        {
            context.Entry(category).State = EntityState.Detached;
            throw new DuplicateCategoryException();
        }
    }
}
