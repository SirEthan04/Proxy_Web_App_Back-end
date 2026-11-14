using System.Data;
using System.Text;
using BodeGo.API.IAM.Application.Services;
using BodeGo.API.IAM.Domain.Model.Aggregates;
using BodeGo.API.IAM.Domain.Model.ValueObjects;
using BodeGo.API.Shared.Infrastructure.Persistence.EFC;
using Microsoft.EntityFrameworkCore;
namespace BodeGo.API.IAM.Infrastructure.Persistence.EFC;

public static class AdministratorInitializer
{
    public static async Task ReportAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BodeGoDbContext>();
        var active = await db.Users.AnyAsync(u => u.Active && db.Roles.Any(r => r.Id == u.RoleId && r.Name == "Administrador"));
        var existing = await db.Users.AnyAsync(u => db.Roles.Any(r => r.Id == u.RoleId && r.Name == "Administrador"));
        Console.WriteLine($"Administrador existente: {existing}; administrador activo: {active}. Consulta de solo lectura.");
    }

    // Acción manual de consola; nunca se ejecuta durante el arranque normal.
    public static async Task InitializeAsync(IServiceProvider services)
    {
        if (Console.IsInputRedirected) throw new InvalidOperationException("Se requiere una terminal interactiva.");
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BodeGoDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var role = await db.Roles.SingleOrDefaultAsync(r => r.Name == "Administrador")
            ?? throw new InvalidOperationException("Inicialice y revise primero el catálogo de roles.");
        if (await db.Users.AnyAsync(u => u.RoleId == role.Id))
            throw new InvalidOperationException("Ya existe un administrador, incluso si está inactivo. No se modificará ninguna cuenta.");
        var database = db.Database.GetDbConnection().Database;
        Console.WriteLine($"Base destino: {database}. Creará una cuenta nueva sin cambiar cuentas existentes.");
        Console.WriteLine($"Escriba CREAR ADMINISTRADOR EN {database} para continuar:");
        if (Console.ReadLine() != $"CREAR ADMINISTRADOR EN {database}") return;
        Console.Write("Nombre: ");
        var name = Console.ReadLine() ?? "";
        Console.Write("Correo: ");
        var email = new EmailAddress(Console.ReadLine() ?? "");
        if (await db.Users.AnyAsync(u => u.Email == email))
            throw new InvalidOperationException("El correo ya existe. No se cambiará su rol ni contraseña.");
        Console.Write("Contraseña (no visible): ");
        var password = ReadPassword();
        Console.Write("Repita la contraseña: ");
        var confirmation = ReadPassword();
        if (password.Length is < 12 or > 128 || password != confirmation)
            throw new InvalidOperationException("Las contraseñas deben coincidir y tener entre 12 y 128 caracteres.");
        var hash = scope.ServiceProvider.GetRequiredService<IPasswordHashService>().Hash(password);
        db.Users.Add(new User(role.Id, name, email, hash));
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        Console.WriteLine("Administrador creado. No se muestran sus credenciales.");
    }

    private static string ReadPassword()
    {
        var text = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); return text.ToString(); }
            if (key.Key == ConsoleKey.Backspace && text.Length > 0) text.Length--;
            else if (!char.IsControl(key.KeyChar) && text.Length < 129) text.Append(key.KeyChar);
        }
    }
}
