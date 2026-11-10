using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
namespace BodeGo.API.Operations.Interfaces.REST;
internal static class OperationsValidationResponse
{
    public static void Configure(ApiBehaviorOptions options)
    {
        var previous = options.InvalidModelStateResponseFactory;
        options.InvalidModelStateResponseFactory = context =>
        {
            if (context.ActionDescriptor is not ControllerActionDescriptor action ||
                action.ControllerTypeInfo.Namespace != typeof(WastesController).Namespace)
                return previous(context);
            var errors = context.ModelState.ToDictionary(x => x.Key,
                x => new[] { "Valor inválido u obligatorio. Revise el JSON, los tipos y el formato de los identificadores." });
            return new BadRequestObjectResult(new ValidationProblemDetails(errors)
            {
                Status = 400, Title = "Se produjeron errores de validación."
            });
        };
    }
}
