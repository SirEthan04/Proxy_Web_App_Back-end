using BodeGo.API.Commercial.Application.Model.Results;
using BodeGo.API.Commercial.Domain.Model.Commands;
using BodeGo.API.Commercial.Domain.Model.Queries;
namespace BodeGo.API.Commercial.Application.Services;
public interface IPurchaseCommandService { Task<PurchaseResult> Handle(CreatePurchaseCommand command, CancellationToken cancellationToken = default); }
public interface ISaleCommandService { Task<SaleResult> Handle(CreateSaleCommand command, CancellationToken cancellationToken = default); }
public interface IPurchaseQueryService
{
    Task<IReadOnlyList<PurchaseResult>> Handle(GetPurchasesQuery query, CancellationToken cancellationToken = default);
    Task<PurchaseResult?> Handle(GetPurchaseByIdQuery query, CancellationToken cancellationToken = default);
}
public interface ISaleQueryService
{
    Task<IReadOnlyList<SaleResult>> Handle(GetSalesQuery query, CancellationToken cancellationToken = default);
    Task<SaleResult?> Handle(GetSaleByIdQuery query, CancellationToken cancellationToken = default);
}
