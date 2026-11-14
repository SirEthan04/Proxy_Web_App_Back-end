using BodeGo.API.Shared.Domain;
namespace BodeGo.API.IAM.Domain.Model.Entities;
public class Role
{
    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
    private Role() { }
    public Role(int id, string name)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        Id = id;
        Name = Validation.Text(name, 30, nameof(name));
    }
}
