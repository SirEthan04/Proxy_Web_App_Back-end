using BodeGo.API.Reporting.Application.Model.ReadModels;
using BodeGo.API.Reporting.Interfaces.REST.Resources;
namespace BodeGo.API.Reporting.Interfaces.REST.Transform;
public static class TopProductResourceAssembler
{
    public static TopProductResource From(TopProductReadModel product) => new(product.ProductId, product.ProductName, product.QuantitySold);
}
