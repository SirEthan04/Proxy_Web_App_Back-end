using BodeGo.API.Commercial.Application.Internal.CommandServices;
using BodeGo.API.Commercial.Application.Model.Results;
using BodeGo.API.Commercial.Application.Services;
using BodeGo.API.Commercial.Domain.Model.Commands;
using BodeGo.API.Commercial.Domain.Model.Aggregates;
using BodeGo.API.Commercial.Domain.Model.ValueObjects;
using BodeGo.API.IAM.Application.Services;

// Sólo sustituye persistencia de creación: las confirmaciones usan los servicios reales.
internal sealed class TestCommercialCommands(TestRepositories store, ICurrentUser currentUser) : IPurchaseCommandService, ISaleCommandService
{
    public Task<PurchaseResult> Handle(CreatePurchaseCommand command, CancellationToken ct = default)
    {
        CommercialIdentity.Creating(currentUser.User, command.UserId);
        var purchase = new Purchase(command.UserId);
        foreach (var item in command.Items) purchase.AddItem(item.ProductId, item.LotId, new Quantity(item.Quantity), new Money(item.UnitAmount));
        store.Purchases.Add(purchase);
        return Task.FromResult(PurchaseResult.From(purchase));
    }
    public Task<SaleResult> Handle(CreateSaleCommand command, CancellationToken ct = default)
    {
        CommercialIdentity.Creating(currentUser.User, command.UserId);
        var sale = new Sale(command.UserId);
        foreach (var item in command.Items) sale.AddItem(item.ProductId, item.LotId, new Quantity(item.Quantity), new Money(item.UnitAmount));
        store.Sales.Add(sale);
        return Task.FromResult(SaleResult.From(sale));
    }
}
