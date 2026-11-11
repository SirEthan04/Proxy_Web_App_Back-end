namespace BodeGo.API.Operations.Domain.Exceptions;
public class OperationsException(string message, bool conflict = false, bool forbidden = false) : Exception(message)
{
    public bool Conflict { get; } = conflict;
    public bool Forbidden { get; } = forbidden;
}
