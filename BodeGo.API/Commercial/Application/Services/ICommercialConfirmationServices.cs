using BodeGo.API.Commercial.Application.Model.Results;
using BodeGo.API.Commercial.Domain.Model.Commands;
namespace BodeGo.API.Commercial.Application.Services;
public interface IPurchaseConfirmationService
{
    Task<PurchaseResult> Handle(ConfirmPurchaseCommand command, CancellationToken ct = default);
}
public interface ISaleConfirmationService
{
    Task<SaleResult> Handle(ConfirmSaleCommand command, CancellationToken ct = default);
}
