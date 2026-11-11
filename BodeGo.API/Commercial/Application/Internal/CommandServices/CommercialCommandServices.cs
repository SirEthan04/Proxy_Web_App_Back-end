using BodeGo.API.Commercial.Application.Model.Results;
using BodeGo.API.Commercial.Application.Services;
using BodeGo.API.Commercial.Domain.Model.Aggregates;
using BodeGo.API.Commercial.Domain.Model.Commands;
using BodeGo.API.Commercial.Domain.Model.ValueObjects;
using BodeGo.API.Commercial.Domain.Repositories;
using BodeGo.API.Shared.Infrastructure.Persistence.EFC;
using Microsoft.EntityFrameworkCore;
using BodeGo.API.IAM.Application.Services;
namespace BodeGo.API.Commercial.Application.Internal.CommandServices;
public class PurchaseCommandService(IPurchaseRepository purchases, BodeGoDbContext db, ICurrentUser currentUser) : IPurchaseCommandService
{
    public async Task<PurchaseResult> Handle(CreatePurchaseCommand command, CancellationToken cancellationToken = default)
    {
        CommercialIdentity.Creating(currentUser.User, command.UserId);
        ValidateItems(command.UserId, command.Items);
        await ValidateReferences(command.UserId, command.Items, cancellationToken);
        var purchase = new Purchase(command.UserId);
        foreach (var item in command.Items) purchase.AddItem(item.ProductId, item.LotId, new Quantity(item.Quantity), new Money(item.UnitAmount));
        await purchases.AddAsync(purchase, cancellationToken);
        return PurchaseResult.From(purchase);
    }
    internal async Task ValidateReferences(Guid userId, IReadOnlyList<CommercialItemCommand> items, CancellationToken ct)
    {
        if (!await db.Users.AnyAsync(x => x.Id == userId && x.Active, ct)) throw new ArgumentException("El usuario no existe o está inactivo.");
        foreach (var item in items)
        {
            if (!await db.Products.AnyAsync(x => x.Id == item.ProductId, ct)) throw new ArgumentException("No se encontró uno de los productos indicados.");
            if (item.LotId is Guid lotId && !await db.ProductLots.AnyAsync(x => x.Id == lotId && x.ProductId == item.ProductId, ct))
                throw new ArgumentException("El lote indicado no existe o no pertenece al producto.");
        }
    }
    internal static void ValidateItems(Guid userId, IReadOnlyList<CommercialItemCommand> items)
    {
        if (userId == Guid.Empty) throw new ArgumentException("El identificador del usuario es obligatorio.");
        if (items is null || items.Count == 0) throw new ArgumentException("Debe incluir al menos un detalle.");
        foreach (var item in items) { _ = new Quantity(item.Quantity); _ = new Money(item.UnitAmount); if (item.ProductId == Guid.Empty) throw new ArgumentException("El identificador del producto es obligatorio."); }
    }
}
public class SaleCommandService(ISaleRepository sales, PurchaseCommandService validator, ICurrentUser currentUser) : ISaleCommandService
{
    public async Task<SaleResult> Handle(CreateSaleCommand command, CancellationToken cancellationToken = default)
    {
        CommercialIdentity.Creating(currentUser.User, command.UserId);
        PurchaseCommandService.ValidateItems(command.UserId, command.Items);
        await validator.ValidateReferences(command.UserId, command.Items, cancellationToken);
        var sale = new Sale(command.UserId);
        foreach (var item in command.Items) sale.AddItem(item.ProductId, item.LotId, new Quantity(item.Quantity), new Money(item.UnitAmount));
        await sales.AddAsync(sale, cancellationToken);
        return SaleResult.From(sale);
    }
}
