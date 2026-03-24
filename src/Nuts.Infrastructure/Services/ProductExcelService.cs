using System.Net;
using ClosedXML.Excel;
using Nuts.Application.Common;
using Nuts.Application.Products;
using Nuts.Domain.Entities;

namespace Nuts.Infrastructure.Services;

internal sealed class ProductExcelService(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork) : IProductExcelService
{
    public async Task<byte[]> ExportAsync(CancellationToken ct = default)
    {
        var products = await productRepository.GetAllAsync(ct);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Products");

        // Headers
        string[] headers = ["Name", "Description", "Price", "Origin", "Category", "IsAvailable", "SortOrder", "ImagePath"];
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
        }

        // Data rows
        for (var row = 0; row < products.Count; row++)
        {
            var p = products[row];
            var r = row + 2;
            ws.Cell(r, 1).Value = WebUtility.HtmlDecode(p.Name);
            ws.Cell(r, 2).Value = WebUtility.HtmlDecode(p.Description);
            ws.Cell(r, 3).Value = p.Price;
            ws.Cell(r, 3).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(r, 4).Value = WebUtility.HtmlDecode(p.Origin);
            ws.Cell(r, 5).Value = WebUtility.HtmlDecode(p.Category);
            ws.Cell(r, 6).Value = p.IsAvailable ? "Да" : "Нет";
            ws.Cell(r, 7).Value = p.SortOrder;
            ws.Cell(r, 8).Value = p.ImagePath ?? string.Empty;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<ProductImportResult> ImportAsync(Stream excelStream, CancellationToken ct = default)
    {
        using var workbook = new XLWorkbook(excelStream);
        var ws = workbook.Worksheet(1);
        var rows = ws.RangeUsed()?.RowsUsed().Skip(1).ToList() ?? [];

        var created = 0;
        var updated = 0;
        var failed = 0;
        var errors = new List<string>();

        foreach (var row in rows)
        {
            var rowNum = row.RowNumber();
            try
            {
                var name = row.Cell(1).GetString().Trim();
                var description = row.Cell(2).GetString().Trim();
                var price = row.Cell(3).GetValue<decimal>();
                var origin = row.Cell(4).GetString().Trim();
                var category = row.Cell(5).GetString().Trim();
                var isAvailableStr = row.Cell(6).GetString().Trim();
                var isAvailable = !string.Equals(isAvailableStr, "Нет", StringComparison.OrdinalIgnoreCase);
                var sortOrder = row.Cell(7).IsEmpty() ? 0 : row.Cell(7).GetValue<int>();

                if (string.IsNullOrWhiteSpace(name))
                {
                    failed++;
                    errors.Add($"Row {rowNum}: Name is empty.");
                    continue;
                }

                // Search by HtmlEncoded name (as stored in DB)
                var encodedName = WebUtility.HtmlEncode(name.Trim());
                var existing = await productRepository.GetByNameAsync(encodedName, ct);

                if (existing is not null)
                {
                    var updateResult = existing.Update(name, description, price, origin, category, isAvailable, sortOrder);
                    if (updateResult.IsFailure)
                    {
                        failed++;
                        errors.Add($"Row {rowNum}: {updateResult.Error.Message}");
                        continue;
                    }
                    updated++;
                }
                else
                {
                    var createResult = Product.Create(name, description, price, origin, category);
                    if (createResult.IsFailure)
                    {
                        failed++;
                        errors.Add($"Row {rowNum}: {createResult.Error.Message}");
                        continue;
                    }
                    productRepository.Add(createResult.Value);
                    created++;
                }
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"Row {rowNum}: {ex.Message}");
            }
        }

        await unitOfWork.SaveChangesAsync(ct);

        return new ProductImportResult(created, updated, failed, errors);
    }

    public byte[] GenerateTemplate()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Products");

        string[] headers = ["Name", "Description", "Price", "Origin", "Category", "IsAvailable", "SortOrder"];
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
        }

        // Example row in italic
        ws.Cell(2, 1).Value = "Macadamia Premium";
        ws.Cell(2, 2).Value = "Premium quality macadamia nuts";
        ws.Cell(2, 3).Value = 2500;
        ws.Cell(2, 3).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(2, 4).Value = "Australia";
        ws.Cell(2, 5).Value = "Орехи";
        ws.Cell(2, 6).Value = "Да";
        ws.Cell(2, 7).Value = 0;

        for (var i = 1; i <= 7; i++)
        {
            ws.Cell(2, i).Style.Font.Italic = true;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }
}
