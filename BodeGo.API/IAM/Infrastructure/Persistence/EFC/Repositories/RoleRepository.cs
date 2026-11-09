using BodeGo.API.IAM.Domain.Model.Entities;
using BodeGo.API.IAM.Domain.Repositories;
using BodeGo.API.Shared.Infrastructure.Persistence.EFC;
using Microsoft.EntityFrameworkCore;
namespace BodeGo.API.IAM.Infrastructure.Persistence.EFC.Repositories;

public class RoleRepository(BodeGoDbContext context) : IRoleRepository
{
    public Task<Role?> FindByIdAsync(int id, CancellationToken cancellationToken = default) =>
        context.Roles.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Role>> ListAsync(CancellationToken cancellationToken = default) =>
        await context.Roles.AsNoTracking().OrderBy(r => r.Id).ToListAsync(cancellationToken);
}
