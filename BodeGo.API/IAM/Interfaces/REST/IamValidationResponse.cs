using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace BodeGo.API.IAM.Interfaces.REST;

internal static class IamValidationResponse
{
    public static void Configure(ApiBehaviorOptions options)
    {
        var defaultFactory = options.InvalidModelStateResponseFactory;
        options.InvalidModelStateResponseFactory = context =>
        {
            if (context.ActionDescriptor is not ControllerActionDescriptor action ||
                action.ControllerTypeInfo.Namespace != typeof(UsersController).Namespace)
                return defaultFactory(context);

            var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
            var problem = factory.CreateValidationProblemDetails(context.HttpContext, context.ModelState,
                statusCode: 400, title: "Se produjeron uno o más errores de validación.");

            // Los errores de conversión y JSON provienen del model binding, no de las anotaciones.
            foreach (var key in problem.Errors.Keys.ToArray())
            {
                if (key.Equals("id", StringComparison.OrdinalIgnoreCase))
                    problem.Errors[key] = ["El identificador no es válido."];
                else if (key.StartsWith('$'))
                    problem.Errors[key] = ["El JSON de la solicitud no es válido. Revise los tipos y el formato de los campos."];
                else if (key is "" or "resource")
                    problem.Errors[key] = ["El cuerpo de la solicitud es obligatorio y debe contener un JSON válido."];
            }

            return new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
        };
    }
}
