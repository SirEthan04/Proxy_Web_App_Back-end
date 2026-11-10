using BodeGo.API.Commercial.Domain.Exceptions;
namespace BodeGo.API.Commercial.Interfaces.REST;
internal static class ConfirmationHttp
{
    public static int StatusCode(CommercialConfirmationException exception) => exception.Failure switch
    {
        ConfirmationFailure.NotFound => 404,
        ConfirmationFailure.Conflict => 409,
        ConfirmationFailure.Unavailable => 503,
        _ => 400
    };
}
