using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
namespace BodeGo.API.IAM.Infrastructure.Authentication;

public static class IamOpenApi
{
    public static void Configure(OpenApiOptions options)
    {
        options.AddDocumentTransformer((document, context, ct) =>
        {
            document.Components ??= new();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
                Description = "Token obtenido en POST /api/v1/auth/login." };
            return Task.CompletedTask;
        });
        options.AddOperationTransformer((operation, context, ct) =>
        {
            var metadata = context.Description.ActionDescriptor.EndpointMetadata;
            if (metadata.OfType<IAuthorizeData>().Any() && !metadata.OfType<IAllowAnonymous>().Any())
            {
                operation.Security ??= [];
                operation.Security.Add(new OpenApiSecurityRequirement
                { [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = [] });
                operation.Responses ??= new();
                operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Debe iniciar sesión." });
                operation.Responses.TryAdd("403", new OpenApiResponse { Description = "No tiene permiso." });
            }
            return Task.CompletedTask;
        });
    }
}
