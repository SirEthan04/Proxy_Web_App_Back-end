using BodeGo.API.Shared.Infrastructure.Persistence.EFC;
using BodeGo.API.IAM.Infrastructure;
using BodeGo.API.Inventory.Infrastructure;
using BodeGo.API.Operations.Infrastructure;
using BodeGo.API.Commercial.Infrastructure;
using BodeGo.API.IAM.Infrastructure.Persistence.EFC;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using BodeGo.API.Monitoring.Infrastructure;
using BodeGo.API.Reporting.Infrastructure;
using BodeGo.API.IAM.Infrastructure.Authentication;
using BodeGo.API.Shared.Infrastructure.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddBodeGoHosting(builder.Configuration, builder.Environment);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "BodeGo API";
        return Task.CompletedTask;
    });
    IamOpenApi.Configure(options);
});
builder.Services.AddControllers();
builder.Services.AddIam();
builder.Services.AddIamAuthentication(builder.Configuration);
builder.Services.AddInventory();
builder.Services.AddCommercial();
builder.Services.AddOperations();
builder.Services.AddMonitoring();
builder.Services.AddReporting();
builder.Services.AddDbContext<BodeGoDbContext>(options =>
{
    var connection = BodeGoHosting.DatabaseConnection(builder.Configuration, builder.Environment.IsDevelopment());
    options.UseMySQL(connection);
});

var app = builder.Build();
if (args.Contains("--check-iam-admin"))
{
    try { await AdministratorInitializer.ReportAsync(app.Services); }
    catch
    {
        Console.Error.WriteLine("No se pudo verificar el administrador. Revise la conexión local sin mostrar credenciales.");
        Environment.ExitCode = 1;
    }
    return;
}
if (args.Contains("--initialize-iam-admin"))
{
    if (!app.Environment.IsDevelopment())
        throw new InvalidOperationException("La inicialización manual requiere Development.");
    await AdministratorInitializer.InitializeAsync(app.Services);
    return;
}
if (args.Contains("--initialize-iam-roles"))
{
    if (!app.Environment.IsDevelopment())
        throw new InvalidOperationException("La inicializacion de roles requiere Development.");
    await app.Services.InitializeIamRolesAsync();
    return;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("BodeGo API"));
}

app.UseBodeGoTransport();
app.UseRouting();
app.UseCors(BodeGoHosting.FrontendPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
