using Microsoft.AspNetCore.Http.HttpResults;
using Nuts.Application.Common;
using Nuts.Application.Products;
using Nuts.Domain.Entities;

namespace Nuts.Api.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        // Public
        var publicGroup = app.MapGroup("/api/products").WithTags("Products");

        publicGroup.MapGet("/", async (IProductRepository repo, CancellationToken ct) =>
        {
            var products = await repo.GetAvailableAsync(ct);
            return TypedResults.Ok(products.Select(p => new ProductDto(
                p.Id, p.Name, p.Description, p.ImagePath, p.Price, p.Origin, p.Category, p.SortOrder,
                p.Variants.Select(v => new VariantDto(v.Id, v.Weight, v.Price, v.SortOrder)).ToList())));
        });

        publicGroup.MapGet("/by-name", async Task<Results<Ok<ProductDto>, NotFound>> (
            string name, IProductRepository repo, CancellationToken ct) =>
        {
            var product = await repo.GetByNameAsync(System.Net.WebUtility.HtmlEncode(name), ct);
            if (product is null || !product.IsAvailable) return TypedResults.NotFound();
            return TypedResults.Ok(new ProductDto(
                product.Id, product.Name, product.Description, product.ImagePath,
                product.Price, product.Origin, product.Category, product.SortOrder,
                product.Variants.Select(v => new VariantDto(v.Id, v.Weight, v.Price, v.SortOrder)).ToList()));
        });

        publicGroup.MapGet("/{id:guid}", async Task<Results<Ok<ProductDto>, NotFound>> (
            Guid id, IProductRepository repo, CancellationToken ct) =>
        {
            var product = await repo.GetByIdAsync(id, ct);
            if (product is null || !product.IsAvailable) return TypedResults.NotFound();
            return TypedResults.Ok(new ProductDto(
                product.Id, product.Name, product.Description, product.ImagePath,
                product.Price, product.Origin, product.Category, product.SortOrder,
                product.Variants.Select(v => new VariantDto(v.Id, v.Weight, v.Price, v.SortOrder)).ToList()));
        });

        // Admin
        var adminGroup = app.MapGroup("/api/admin/products")
            .WithTags("Admin — Products")
            .RequireAuthorization("Admin");

        adminGroup.MapGet("/export", async (IProductExcelService excelService, CancellationToken ct) =>
        {
            var bytes = await excelService.ExportAsync(ct);
            return Results.File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"products_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx");
        });

        adminGroup.MapPost("/import", async (IFormFile file, IProductExcelService excelService, CancellationToken ct) =>
        {
            if (file is null || file.Length == 0)
                return Results.BadRequest(new { error = "File is required." });

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest(new { error = "Only .xlsx files are supported." });

            if (file.Length > 5 * 1024 * 1024)
                return Results.BadRequest(new { error = "File must be less than 5 MB." });

            using var stream = file.OpenReadStream();
            var result = await excelService.ImportAsync(stream, ct);
            return Results.Ok(result);
        }).DisableAntiforgery();

        adminGroup.MapGet("/template", (IProductExcelService excelService) =>
        {
            var bytes = excelService.GenerateTemplate();
            return Results.File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "products_template.xlsx");
        });

        adminGroup.MapGet("/", async (IProductRepository repo, CancellationToken ct) =>
        {
            var products = await repo.GetAllAsync(ct);
            return TypedResults.Ok(products.Select(p => new AdminProductDto(
                p.Id, p.Name, p.Description, p.ImagePath, p.Price,
                p.Origin, p.Category, p.IsAvailable, p.SortOrder, p.CreatedAt, p.UpdatedAt,
                p.Variants.Select(v => new VariantDto(v.Id, v.Weight, v.Price, v.SortOrder)).ToList())));
        });

        adminGroup.MapPost("/", async Task<Results<Created<AdminProductDto>, BadRequest<ProblemHttpResult>>> (
            CreateProductRequest req, IProductRepository repo, IUnitOfWork uow, CancellationToken ct) =>
        {
            var result = Product.Create(req.Name, req.Description, req.Price, req.Origin, req.Category);
            return await result.Match<Task<Results<Created<AdminProductDto>, BadRequest<ProblemHttpResult>>>>(
                async product =>
                {
                    if (req.Variants is { Count: > 0 })
                    {
                        foreach (var v in req.Variants)
                            product.AddVariant(v.Weight, v.Price, v.SortOrder);
                    }
                    repo.Add(product);
                    await uow.SaveChangesAsync(ct);
                    var dto = new AdminProductDto(product.Id, product.Name, product.Description,
                        product.ImagePath, product.Price, product.Origin, product.Category,
                        product.IsAvailable, product.SortOrder, product.CreatedAt, product.UpdatedAt,
                        product.Variants.Select(v => new VariantDto(v.Id, v.Weight, v.Price, v.SortOrder)).ToList());
                    return TypedResults.Created($"/api/admin/products/{product.Id}", dto);
                },
                error => Task.FromResult<Results<Created<AdminProductDto>, BadRequest<ProblemHttpResult>>>(
                    TypedResults.BadRequest(TypedResults.Problem(error.Message, statusCode: 400))));
        });

        adminGroup.MapPut("/{id:guid}", async Task<Results<Ok<AdminProductDto>, NotFound, BadRequest<ProblemHttpResult>>> (
            Guid id, UpdateProductRequest req, IProductRepository repo, IUnitOfWork uow, CancellationToken ct) =>
        {
            var product = await repo.GetByIdAsync(id, ct);
            if (product is null) return TypedResults.NotFound();

            var updateResult = product.Update(req.Name, req.Description, req.Price, req.Origin, req.Category, req.IsAvailable, req.SortOrder);
            if (updateResult.IsFailure)
                return TypedResults.BadRequest(TypedResults.Problem(updateResult.Error.Message, statusCode: 400));

            if (req.Variants is not null)
            {
                // Delete old variants via raw SQL to avoid EF tracking conflicts
                await repo.RemoveVariantsAsync(product.Id, ct);
                product.ClearVariants();

                // Add new variants
                foreach (var v in req.Variants)
                    product.AddVariant(v.Weight, v.Price, v.SortOrder);
            }

            await uow.SaveChangesAsync(ct);

            var dto = new AdminProductDto(product.Id, product.Name, product.Description,
                product.ImagePath, product.Price, product.Origin, product.Category,
                product.IsAvailable, product.SortOrder, product.CreatedAt, product.UpdatedAt,
                product.Variants.Select(v => new VariantDto(v.Id, v.Weight, v.Price, v.SortOrder)).ToList());
            return TypedResults.Ok(dto);
        });

        adminGroup.MapPost("/{id:guid}/image", async Task<Results<Ok<string>, NotFound, BadRequest<string>>> (
            Guid id, IFormFile file, IProductRepository repo, IUnitOfWork uow, CancellationToken ct) =>
        {
            var product = await repo.GetByIdAsync(id, ct);
            if (product is null) return TypedResults.NotFound();

            if (file.Length > 5 * 1024 * 1024)
                return TypedResults.BadRequest("File must be less than 5 MB.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
                return TypedResults.BadRequest("Only JPG, PNG, WEBP files are supported.");

            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            Directory.CreateDirectory(uploadsDir);

            var fileName = $"{id}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, ct);
            }

            product.SetImage($"/uploads/{fileName}");
            await uow.SaveChangesAsync(ct);

            return TypedResults.Ok($"/uploads/{fileName}");
        }).DisableAntiforgery();

        adminGroup.MapDelete("/{id:guid}/image", async Task<Results<Ok, NotFound>> (
            Guid id, IProductRepository repo, IUnitOfWork uow, CancellationToken ct) =>
        {
            var product = await repo.GetByIdAsync(id, ct);
            if (product is null) return TypedResults.NotFound();

            if (product.ImagePath?.StartsWith("/uploads/") == true)
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", product.ImagePath.TrimStart('/'));
                if (File.Exists(filePath)) File.Delete(filePath);
            }

            product.SetImage(null);
            await uow.SaveChangesAsync(ct);
            return TypedResults.Ok();
        });

        adminGroup.MapDelete("/{id:guid}", async Task<Results<NoContent, NotFound>> (
            Guid id, IProductRepository repo, IUnitOfWork uow, CancellationToken ct) =>
        {
            var product = await repo.GetByIdAsync(id, ct);
            if (product is null) return TypedResults.NotFound();
            repo.Remove(product);
            await uow.SaveChangesAsync(ct);
            return TypedResults.NoContent();
        });
    }
}

public sealed record VariantDto(Guid Id, string Weight, decimal Price, int SortOrder);

public sealed record ProductDto(
    Guid Id, string Name, string Description, string? ImagePath,
    decimal Price, string Origin, string Category, int SortOrder,
    List<VariantDto> Variants);

public sealed record AdminProductDto(
    Guid Id, string Name, string Description, string? ImagePath,
    decimal Price, string Origin, string Category, bool IsAvailable,
    int SortOrder, DateTime CreatedAt, DateTime? UpdatedAt,
    List<VariantDto> Variants);

public sealed record CreateVariantRequest
{
    public required string Weight { get; init; }
    public required decimal Price { get; init; }
    public int SortOrder { get; init; }
}

public sealed record CreateProductRequest
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required decimal Price { get; init; }
    public required string Origin { get; init; }
    public required string Category { get; init; }
    public List<CreateVariantRequest>? Variants { get; init; }
}

public sealed record UpdateProductRequest
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required decimal Price { get; init; }
    public required string Origin { get; init; }
    public required string Category { get; init; }
    public required bool IsAvailable { get; init; }
    public required int SortOrder { get; init; }
    public List<CreateVariantRequest>? Variants { get; init; }
}
