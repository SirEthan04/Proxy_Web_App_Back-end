using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
namespace BodeGo.API.Inventory.Interfaces.REST;
internal static class InventoryValidationResponse
{
    public static void Configure(ApiBehaviorOptions options)
    {
        var previousFactory = options.InvalidModelStateResponseFactory;
        options.InvalidModelStateResponseFactory = context =>
        {
            if (context.ActionDescriptor is not ControllerActionDescriptor action ||
                action.ControllerTypeInfo.Namespace != typeof(ProductsController).Namespace)
                return previousFactory(context);
            var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
            var problem = factory.CreateValidationProblemDetails(context.HttpContext, context.ModelState,
                statusCode: 400, title: "Se produjeron uno o más errores de validación.");
            foreach (var key in problem.Errors.Keys.ToArray())
            {
                if (key.Equals("id", StringComparison.OrdinalIgnoreCase))
                    problem.Errors[key] = ["El identificador no es válido."];
                else if (key.StartsWith('$'))
                    problem.Errors[key] = ["El JSON de la solicitud no es válido. Revise los tipos, campos y formato."];
                else if (key is "" or "resource")
                    problem.Errors[key] = ["El cuerpo de la solicitud es obligatorio y debe contener un JSON válido."];
            }
            return new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
        };
    }
}
