using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using BodeGo.API.IAM.Application.Internal.CommandServices;
using BodeGo.API.IAM.Application.Internal.QueryServices;
using BodeGo.API.IAM.Domain.Exceptions;
using BodeGo.API.IAM.Domain.Model.Aggregates;
using BodeGo.API.IAM.Domain.Model.Commands;
using BodeGo.API.IAM.Domain.Model.Entities;
using BodeGo.API.IAM.Domain.Model.Queries;
using BodeGo.API.IAM.Domain.Model.ValueObjects;
using BodeGo.API.IAM.Domain.Repositories;
using BodeGo.API.IAM.Infrastructure.Hashing;
using BodeGo.API.IAM.Infrastructure.Persistence.EFC;
using BodeGo.API.Shared.Infrastructure.Persistence.EFC;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;

var checks = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
    checks++;
}
async Task Reject<T>(Func<Task> action) where T : Exception
{
    try { await action(); }
    catch (T) { checks++; return; }
    throw new InvalidOperationException($"Expected {typeof(T).Name}.");
}

// Unit checks use explicit in-memory doubles, not evidence of MySQL integration.
var passwords = new PasswordHashService();
var memory = new MemoryRepositories();
var commands = new UserCommandService(memory, memory, passwords);
var temporaryPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
var created = await commands.Handle(new CreateUserCommand("Test IAM", " Unit@Example.com ", temporaryPassword, 2));
Check(created.Email == "unit@example.com" && created.Active, "Normalization failed.");
var stored = memory.Users.Single();
Check(stored.PasswordHash != temporaryPassword &&
    new PasswordHasher<object>().VerifyHashedPassword(new object(), stored.PasswordHash, temporaryPassword)
        != PasswordVerificationResult.Failed, "Password hash verification failed.");
Check(passwords.Hash(temporaryPassword) != stored.PasswordHash, "Password salts are not distinct.");
await Reject<DuplicateEmailException>(() => commands.Handle(new CreateUserCommand("Test", "UNIT@example.com", temporaryPassword, 2)));
await Reject<RoleNotFoundException>(() => commands.Handle(new CreateUserCommand("Test", "other@example.com", temporaryPassword, 99)));
await Reject<ArgumentException>(() => commands.Handle(new CreateUserCommand(" ", "other@example.com", temporaryPassword, 2)));
await Reject<ArgumentException>(() => commands.Handle(new CreateUserCommand("Test", "invalid", temporaryPassword, 2)));
await Reject<ArgumentException>(() => commands.Handle(new CreateUserCommand("Test", "other@example.com", "short", 2)));
var users = new UserQueryService(memory);
Check((await users.Handle(new GetUsersQuery())).Count == 1, "User listing failed.");
Check(await users.Handle(new GetUserByIdQuery(Guid.NewGuid())) is null, "Missing user was found.");
var roles = new RoleQueryService(memory);
Check((await roles.Handle(new GetRolesQuery())).Count == 2, "Role listing failed.");
Check(await roles.Handle(new GetRoleByIdQuery(99)) is null, "Missing role was found.");
Console.WriteLine($"Unit checks: {checks} passed; no database access.");
if (args.Length == 0) return;

try
{
    using var db = new BodeGoDbContextFactory().CreateDbContext([]);
    var connection = new MySqlConnectionStringBuilder(db.Database.GetConnectionString());
    Check(connection.Server is "localhost" or "127.0.0.1" && connection.Port == 3306 &&
        connection.Database == "bodego_db", "Only the local development database is allowed.");
    Check(!(await db.Database.GetPendingMigrationsAsync()).Any(), "Apply the existing migration before integration checks.");
    Check(await db.Database.CanConnectAsync(), "MySQL connection unavailable.");
    Console.WriteLine("Local bodego_db connection verified; migration is applied.");
    if (args[0] == "--preflight") return;
    if (args.Length != 2 || args[0] != "--integration") throw new InvalidOperationException("Use --integration <local API URL>.");
    var uri = new Uri(args[1]);
    Check(uri.IsLoopback, "Integration checks require a local API.");

    using var provider = new ServiceCollection()
        .AddDbContext<BodeGoDbContext>(options => options.UseMySQL(connection.ConnectionString)).BuildServiceProvider();
    await provider.InitializeIamRolesAsync();
    await provider.InitializeIamRolesAsync();
    Check(await db.Roles.CountAsync(r => r.Id == 1 || r.Id == 2) == 2, "Role initialization failed.");
    using var http = new HttpClient { BaseAddress = uri };
    const string email = "iam.verificacion@bodego.example";
    async Task<JsonElement> GetJson(string path)
    {
        using var response = await http.GetAsync(path);
        Check(response.StatusCode == HttpStatusCode.OK, $"GET {path} failed.");
        var text = await response.Content.ReadAsStringAsync();
        Check(!text.Contains("password", StringComparison.OrdinalIgnoreCase), "Response contains a password field.");
        return JsonDocument.Parse(text).RootElement.Clone();
    }
    async Task ExpectPost(object body, HttpStatusCode expected)
    {
        using var response = await http.PostAsJsonAsync("/api/v1/users", body);
        Check(response.StatusCode == expected, $"POST expected {(int)expected}, received {(int)response.StatusCode}.");
        var text = await response.Content.ReadAsStringAsync();
        Check(!text.Contains(temporaryPassword, StringComparison.Ordinal), "Password leaked in response.");
    }

    var roleList = await GetJson("/api/v1/roles");
    Check(roleList.EnumerateArray().Any(r => r.GetProperty("name").GetString() == "Empleado"), "Employee role missing.");
    await GetJson("/api/v1/roles/1");
    var userList = await GetJson("/api/v1/users");
    var existing = userList.EnumerateArray().Where(u => u.GetProperty("email").GetString() == email).ToArray();
    Guid id;
    if (existing.Length == 0)
    {
        using var response = await http.PostAsJsonAsync("/api/v1/users",
            new { name = "Prueba IAM desarrollo", email, password = temporaryPassword, roleId = 2 });
        Check(response.StatusCode == HttpStatusCode.Created && response.Headers.Location != null, "Registration must return 201 and Location.");
        var text = await response.Content.ReadAsStringAsync();
        Check(!text.Contains("password", StringComparison.OrdinalIgnoreCase) && !text.Contains(temporaryPassword), "Registration exposed a password.");
        id = JsonDocument.Parse(text).RootElement.GetProperty("id").GetGuid();
        var persisted = await db.Users.AsNoTracking().SingleAsync(u => u.Id == id);
        Check(new PasswordHasher<object>().VerifyHashedPassword(new object(), persisted.PasswordHash, temporaryPassword)
            != PasswordVerificationResult.Failed, "The persisted hash cannot verify the submitted password.");
        Console.WriteLine("Development user inserted through POST; persisted hash verified.");
    }
    else
    {
        Check(existing.Length == 1, "Duplicate development users found.");
        id = existing[0].GetProperty("id").GetGuid();
        Console.WriteLine("Existing development user reused; no new registration claimed in this run.");
    }
    var retrieved = await GetJson($"/api/v1/users/{id}");
    Check(retrieved.GetProperty("email").GetString() == email, "User lookup returned different data.");
    var updatedList = await GetJson("/api/v1/users");
    Check(updatedList.EnumerateArray().Count(u => u.GetProperty("email").GetString() == email) == 1, "User is absent or duplicated.");
    await ExpectPost(new { name = "Prueba", email = " IAM.VERIFICACION@BODEGO.EXAMPLE ", password = temporaryPassword, roleId = 2 }, HttpStatusCode.Conflict);
    await ExpectPost(new { name = "Prueba", email = "iam.rol-inexistente@bodego.example", password = temporaryPassword, roleId = int.MaxValue }, HttpStatusCode.BadRequest);
    await ExpectPost(new { name = " ", email, password = temporaryPassword, roleId = 2 }, HttpStatusCode.BadRequest);
    await ExpectPost(new { name = "Prueba", email = "invalid", password = temporaryPassword, roleId = 2 }, HttpStatusCode.BadRequest);
    await ExpectPost(new { name = "Prueba", email, password = "short", roleId = 2 }, HttpStatusCode.BadRequest);
    foreach (var path in new[] { $"/api/v1/users/{Guid.NewGuid()}", "/api/v1/roles/2147483647" })
    {
        using var response = await http.GetAsync(path);
        Check(response.StatusCode == HttpStatusCode.NotFound, "Missing resource must return 404.");
    }
    foreach (var path in new[] { "/api/v1/users/invalid", "/api/v1/users/00000000-0000-0000-0000-000000000000", "/api/v1/roles/0" })
    {
        using var response = await http.GetAsync(path);
        Check(response.StatusCode == HttpStatusCode.BadRequest, "Invalid identifier must return 400.");
    }
    Check(await db.Users.CountAsync(u => u.Email == new EmailAddress(email)) == 1, "MySQL contains duplicate test users.");
    async Task ExpectSpanishPost(object body, HttpStatusCode status, string message, string? key = null)
    {
        using var response = await http.PostAsJsonAsync("/api/v1/users", body);
        Check(response.StatusCode == status, "Unexpected Spanish validation status.");
        var text = await response.Content.ReadAsStringAsync();
        Check(!text.Contains(temporaryPassword) && !text.Contains("passwordHash", StringComparison.OrdinalIgnoreCase)
            && !text.Contains("password_hash", StringComparison.OrdinalIgnoreCase), "Sensitive data leaked in error.");
        var problem = JsonDocument.Parse(text).RootElement;
        if (key is not null)
        {
            Check(problem.GetProperty("title").GetString() == "Se produjeron uno o más errores de validación.", "Validation title is not Spanish.");
            Check(problem.GetProperty("errors").GetProperty(key).EnumerateArray().Any(e => e.GetString() == message), "Expected field message was not found.");
        }
        else Check(problem.GetProperty("title").GetString() == message, "Expected Spanish error title was not found.");
    }
    object Body(string name, string address, string password, int roleId = 2) =>
        new { name, email = address, password, roleId };
    await ExpectSpanishPost(Body("", email, temporaryPassword), HttpStatusCode.BadRequest, "El nombre es obligatorio.", "Name");
    await ExpectSpanishPost(Body("Prueba", "", temporaryPassword), HttpStatusCode.BadRequest, "El correo electrónico es obligatorio.", "Email");
    await ExpectSpanishPost(Body("Prueba", "invalid", temporaryPassword), HttpStatusCode.BadRequest, "El formato del correo electrónico no es válido.");
    await ExpectSpanishPost(Body("Prueba", email, ""), HttpStatusCode.BadRequest, "La contraseña es obligatoria.", "Password");
    await ExpectSpanishPost(Body("Prueba", email, "short"), HttpStatusCode.BadRequest, "La contraseña debe tener al menos 12 caracteres.", "Password");
    await ExpectSpanishPost(Body("Prueba", email, new string('x', 129)), HttpStatusCode.BadRequest, "La contraseña no debe superar los 128 caracteres.", "Password");
    await ExpectSpanishPost(Body(new string('x', 121), email, temporaryPassword), HttpStatusCode.BadRequest, "El nombre no debe superar los 120 caracteres.", "Name");
    await ExpectSpanishPost(Body("Prueba", new string('x', 181), temporaryPassword), HttpStatusCode.BadRequest, "El correo electrónico no debe superar los 180 caracteres.", "Email");
    await ExpectSpanishPost(Body("Prueba", email, temporaryPassword), HttpStatusCode.Conflict, "El correo electrónico ya está registrado.");
    await ExpectSpanishPost(Body("Prueba", email, temporaryPassword, int.MaxValue), HttpStatusCode.BadRequest, "El rol indicado no existe.");
    foreach (var (json, key) in new[] { ("{", "$"), ("{\"roleId\":\"invalid\"}", "$.roleId") })
    {
        using var response = await http.PostAsync("/api/v1/users", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
        Check(response.StatusCode == HttpStatusCode.BadRequest, "Invalid JSON must return 400.");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Check(problem.GetProperty("errors").GetProperty(key)[0].GetString() ==
            "El JSON de la solicitud no es válido. Revise los tipos y el formato de los campos.", "JSON binding error is not Spanish.");
    }
    using (var invalidId = await http.GetAsync("/api/v1/users/invalid"))
    {
        var problem = await invalidId.Content.ReadFromJsonAsync<JsonElement>();
        Check(problem.GetProperty("errors").GetProperty("id")[0].GetString() == "El identificador no es válido.", "Identifier binding error is not Spanish.");
    }
    foreach (var (path, title) in new[] {
        ($"/api/v1/users/{Guid.NewGuid()}", "No se encontró el usuario solicitado."),
        ("/api/v1/roles/2147483647", "No se encontró el rol solicitado.") })
    {
        using var response = await http.GetAsync(path);
        Check(response.StatusCode == HttpStatusCode.NotFound, "Expected localized 404.");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Check(problem.GetProperty("title").GetString() == title, "404 title is not Spanish.");
    }
    const string spanishTestEmail = "iam.validacion-es@bodego.example";
    if (!await db.Users.AnyAsync(u => u.Email == new EmailAddress(spanishTestEmail)))
    {
        using var response = await http.PostAsJsonAsync("/api/v1/users", Body("Prueba validaciones español", spanishTestEmail, temporaryPassword));
        Check(response.StatusCode == HttpStatusCode.Created && response.Headers.Location != null, "Valid Spanish registration must return 201.");
        var result = await response.Content.ReadAsStringAsync();
        Check(!result.Contains("password", StringComparison.OrdinalIgnoreCase) && !result.Contains(temporaryPassword), "Registration exposed credentials.");
        Console.WriteLine("Spanish validation fixture inserted through POST 201; no existing records modified.");
    }
    else Console.WriteLine("Spanish validation fixture reused; no new POST 201 claimed for this fixture.");
    Check(await db.Users.CountAsync(u => u.Email == new EmailAddress(spanishTestEmail)) == 1, "Spanish fixture was duplicated.");
    await GetJson("/api/v1/roles");
    await GetJson("/api/v1/users");
    Console.WriteLine($"OK: {checks} total checks, including HTTP and real MySQL. No records deleted.");
}
catch (Exception exception)
{
    // Never print connection strings, passwords or SQL exception details.
    Console.Error.WriteLine($"Integration/preflight failed ({exception.GetType().Name}). No integration success claimed.");
    Environment.ExitCode = 1;
}

sealed class MemoryRepositories : IUserRepository, IRoleRepository
{
    public List<User> Users { get; } = [];
    private readonly Role[] _roles = [new(1, "Administrador"), new(2, "Empleado")];
    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Users.SingleOrDefault(u => u.Id == id));
    public Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<User>>(Users.ToArray());
    public Task<bool> ExistsByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default) => Task.FromResult(Users.Any(u => u.Email == email));
    public Task AddAsync(User user, CancellationToken cancellationToken = default) { Users.Add(user); return Task.CompletedTask; }
    public Task<Role?> FindByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(_roles.SingleOrDefault(r => r.Id == id));
    Task<IReadOnlyList<Role>> IRoleRepository.ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Role>>(_roles);
}
