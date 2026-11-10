namespace BodeGo.API.Operations.Domain.Exceptions;
public class OperationsException(string message, bool conflict = false) : Exception(message)
{
    public bool Conflict { get; } = conflict;
}
