using BodeGo.API.IAM.Domain.Model.ValueObjects;
using BodeGo.API.Shared.Domain;
namespace BodeGo.API.IAM.Domain.Model.Aggregates;
public class User
{
    public Guid Id { get; private set; }
    public int RoleId { get; private set; }
    public string Name { get; private set; } = null!;
    public EmailAddress Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public bool Active { get; private set; }
    public DateTime CreatedAt { get; private set; }
    private User() { }
    public User(int roleId, string name, EmailAddress email, string passwordHash)
    {
        if (roleId <= 0) throw new ArgumentOutOfRangeException(nameof(roleId));
        Id = Guid.NewGuid();
        RoleId = roleId;
        Name = Validation.Text(name, 120, nameof(name));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        PasswordHash = Validation.Text(passwordHash, 255, nameof(passwordHash));
        Active = true;
        CreatedAt = DateTime.UtcNow;
    }
    public void Deactivate() => Active = false;
}
