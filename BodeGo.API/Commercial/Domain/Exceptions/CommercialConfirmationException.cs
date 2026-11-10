namespace BodeGo.API.Commercial.Domain.Exceptions;
public enum ConfirmationFailure { Invalid, NotFound, Conflict, Unavailable }
public sealed class CommercialConfirmationException(string message, ConfirmationFailure failure = ConfirmationFailure.Invalid)
    : Exception(message)
{
    public ConfirmationFailure Failure { get; } = failure;
}
