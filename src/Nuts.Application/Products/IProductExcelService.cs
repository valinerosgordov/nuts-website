namespace Nuts.Application.Products;

public interface IProductExcelService
{
    Task<byte[]> ExportAsync(CancellationToken ct = default);
    Task<ProductImportResult> ImportAsync(Stream excelStream, CancellationToken ct = default);
    byte[] GenerateTemplate();
}

public sealed record ProductImportResult(int Created, int Updated, int Failed, List<string> Errors);
