using Nuts.Application.Common;
using Nuts.Application.Products;
using Nuts.Infrastructure.Services;

namespace Nuts.Api.Endpoints;

public static class MoySkladEndpoints
{
    public static void MapMoySkladEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/sync")
            .WithTags("Admin — MoySklad Sync")
            .RequireAuthorization("Admin");

        group.MapPost("/moysklad", async (
            IMoySkladService moySklad,
            IProductRepository productRepo,
            IUnitOfWork uow,
            CancellationToken ct) =>
        {
            var msProducts = await moySklad.GetProductsAsync(ct);
            var localProducts = await productRepo.GetAllAsync(ct);

            int synced = 0, notFound = 0;

            foreach (var local in localProducts)
            {
                var decodedName = System.Net.WebUtility.HtmlDecode(local.Name);
                var match = msProducts.FirstOrDefault(m =>
                    string.Equals(m.Name, decodedName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(m.Name, local.Name, StringComparison.OrdinalIgnoreCase));

                if (match is not null && match.Price > 0)
                {
                    local.Update(local.Name, local.Description, match.Price,
                        local.Origin, local.Category, local.IsAvailable, local.SortOrder);
                    synced++;
                }
                else
                {
                    notFound++;
                }
            }

            await uow.SaveChangesAsync(ct);

            return TypedResults.Ok(new SyncResult(synced, notFound, msProducts.Count));
        });
    }
}

public sealed record SyncResult(int Synced, int NotFound, int MoySkladTotal);
