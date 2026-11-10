using BodeGo.API.Operations.Domain.Model.Aggregates;
using BodeGo.API.Operations.Domain.Model.Commands;
namespace BodeGo.API.Operations.Application.Services;
public interface IWasteCommandService
{
    Task<Waste> Handle(CreateWasteCommand command, CancellationToken ct = default);
}
