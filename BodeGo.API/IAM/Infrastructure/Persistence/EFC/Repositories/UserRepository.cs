using BodeGo.API.IAM.Domain.Exceptions;
using BodeGo.API.IAM.Domain.Model.Aggregates;
using BodeGo.API.IAM.Domain.Model.ValueObjects;
using BodeGo.API.IAM.Domain.Repositories;
using BodeGo.API.Shared.Infrastructure.Persistence.EFC;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
namespace BodeGo.API.IAM.Infrastructure.Persistence.EFC.Repositories;

public class UserRepository(BodeGoDbContext context) : IUserRepository
{
    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken = default) =>
        await context.Users.AsNoTracking().OrderBy(u => u.Name).ThenBy(u => u.Id).ToListAsync(cancellationToken);

    public Task<bool> ExistsByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default) =>
        context.Users.AnyAsync(u => u.Email == email, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        context.Users.Add(user);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is MySqlException { Number: 1062 } sql &&
            sql.Message.Contains("IX_users_email", StringComparison.OrdinalIgnoreCase))
        {
            context.Entry(user).State = EntityState.Detached;
            throw new DuplicateEmailException();
        }
        catch (DbUpdateException exception) when (exception.InnerException is MySqlException { Number: 1452 })
        {
            context.Entry(user).State = EntityState.Detached;
            throw new RoleNotFoundException();
        }
    }
}
