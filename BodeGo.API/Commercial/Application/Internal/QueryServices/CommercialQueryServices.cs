using BodeGo.API.Commercial.Application.Model.Results;
using BodeGo.API.Commercial.Application.Services;
using BodeGo.API.Commercial.Domain.Model.Queries;
using BodeGo.API.Commercial.Domain.Repositories;
namespace BodeGo.API.Commercial.Application.Internal.QueryServices;
public class PurchaseQueryService(IPurchaseRepository repo) : IPurchaseQueryService
{
    public async Task<IReadOnlyList<PurchaseResult>> Handle(GetPurchasesQuery query, CancellationToken cancellationToken = default) => (await repo.ListAsync(cancellationToken)).Select(PurchaseResult.From).ToArray();
    public async Task<PurchaseResult?> Handle(GetPurchaseByIdQuery query, CancellationToken cancellationToken = default) { if (query.Id == Guid.Empty) throw new ArgumentException("El identificador no puede estar vacío."); var x = await repo.FindByIdAsync(query.Id, cancellationToken); return x is null ? null : PurchaseResult.From(x); }
}
public class SaleQueryService(ISaleRepository repo) : ISaleQueryService
{
    public async Task<IReadOnlyList<SaleResult>> Handle(GetSalesQuery query, CancellationToken cancellationToken = default) => (await repo.ListAsync(cancellationToken)).Select(SaleResult.From).ToArray();
    public async Task<SaleResult?> Handle(GetSaleByIdQuery query, CancellationToken cancellationToken = default) { if (query.Id == Guid.Empty) throw new ArgumentException("El identificador no puede estar vacío."); var x = await repo.FindByIdAsync(query.Id, cancellationToken); return x is null ? null : SaleResult.From(x); }
}
