using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Nuts.Infrastructure.Services;

public interface IMoySkladService
{
    Task<List<MoySkladProduct>> GetProductsAsync(CancellationToken ct = default);
    Task<string?> CreateCustomerOrderAsync(MoySkladOrder order, CancellationToken ct = default);
}

public sealed record MoySkladProduct(string Id, string Name, string? Article, decimal Price, string? Description);

public sealed record MoySkladOrder(
    string CustomerName,
    string CustomerPhone,
    string? CustomerEmail,
    string? ShippingAddress,
    string? Comment,
    decimal? Discount,
    string? PromoCode,
    List<MoySkladOrderItem> Items);

public sealed record MoySkladOrderItem(string ProductName, string Weight, int Quantity, decimal Price);

public sealed class MoySkladService(HttpClient http) : IMoySkladService
{
    private const string BaseUrl = "https://api.moysklad.ru/api/remap/1.2/";
    private const string OrgId = "fcc24f5d-03b3-11ea-0a80-05c00007bf2d";

    public static void Configure(HttpClient client, string token)
    {
        client.BaseAddress = new Uri(BaseUrl);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
    }

    public async Task<List<MoySkladProduct>> GetProductsAsync(CancellationToken ct)
    {
        var products = new List<MoySkladProduct>();
        int offset = 0;
        const int limit = 100;

        while (true)
        {
            var data = await GetAsync($"entity/product?limit={limit}&offset={offset}", ct);
            var size = data.GetProperty("meta").GetProperty("size").GetInt32();

            foreach (var row in data.GetProperty("rows").EnumerateArray())
            {
                var id = row.GetProperty("id").GetString() ?? "";
                var name = row.GetProperty("name").GetString() ?? "";
                var article = row.TryGetProperty("article", out var art) ? art.GetString() : null;
                var description = row.TryGetProperty("description", out var desc) ? desc.GetString() : null;

                decimal price = 0;
                if (row.TryGetProperty("salePrices", out var prices) && prices.GetArrayLength() > 0)
                    price = prices[0].GetProperty("value").GetDecimal() / 100;

                products.Add(new MoySkladProduct(id, name, article, price, description));
            }

            offset += limit;
            if (offset >= size) break;
        }

        return products;
    }

    public async Task<string?> CreateCustomerOrderAsync(MoySkladOrder order, CancellationToken ct)
    {
        try
        {
            var agentHref = await FindOrCreateCounterpartyAsync(order.CustomerName, order.CustomerPhone, order.CustomerEmail, ct);

            var positions = new List<JsonElement>();
            foreach (var item in order.Items)
            {
                var productHref = await FindProductHrefByNameAsync(item.ProductName, ct);
                if (productHref is null) continue;

                // MoySklad: price = per unit (per kg), quantity = total kg
                // Example: "5 кг" at 5315₽ x1 → qty=5, price=1063₽/кг → total=5315₽
                // Example: "500 г" at 270₽ x2 → qty=1, price=270₽/0.5кг=540₽/кг → total=540₽
                var weightKg = ParseWeightToKg(item.Weight);
                decimal qty;
                long priceKopecks;

                if (weightKg > 0)
                {
                    qty = weightKg * item.Quantity;
                    // Price per kg = total price / weight in kg
                    var pricePerKg = item.Price / weightKg;
                    priceKopecks = (long)(pricePerKg * 100);
                }
                else
                {
                    qty = item.Quantity;
                    priceKopecks = (long)(item.Price * 100);
                }

                var pos = JsonSerializer.SerializeToElement(new
                {
                    quantity = qty,
                    price = priceKopecks,
                    assortment = new { meta = new { href = productHref, type = "product", mediaType = "application/json" } }
                });
                positions.Add(pos);
            }

            var orderBody = new
            {
                organization = new { meta = new { href = $"{BaseUrl}entity/organization/{OrgId}", type = "organization", mediaType = "application/json" } },
                agent = new { meta = new { href = agentHref, type = "counterparty", mediaType = "application/json" } },
                shipmentAddress = order.ShippingAddress ?? "",
                description = BuildDescription(order),
                positions,
                attributes = BuildAttributes(order)
            };

            var json = JsonSerializer.Serialize(orderBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await http.PostAsync("entity/customerorder", content, ct);

            if (response.IsSuccessStatusCode)
            {
                var respData = await ReadResponseAsync(response, ct);
                return respData.GetProperty("id").GetString();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<string> FindOrCreateCounterpartyAsync(string name, string phone, string? email, CancellationToken ct)
    {
        var data = await GetAsync($"entity/counterparty?filter=phone={Uri.EscapeDataString(phone)}&limit=1", ct);

        if (data.GetProperty("rows").GetArrayLength() > 0)
            return data.GetProperty("rows")[0].GetProperty("meta").GetProperty("href").GetString()!;

        var body = JsonSerializer.Serialize(new { name, phone, email = email ?? "" });
        var resp = await http.PostAsync("entity/counterparty", new StringContent(body, Encoding.UTF8, "application/json"), ct);
        var created = await ReadResponseAsync(resp, ct);
        return created.GetProperty("meta").GetProperty("href").GetString()!;
    }

    private async Task<string?> FindProductHrefByNameAsync(string productName, CancellationToken ct)
    {
        // Product names may be HtmlEncoded in our DB — decode before searching MoySklad
        var decodedName = System.Net.WebUtility.HtmlDecode(productName);

        // Try exact match first
        var data = await GetAsync($"entity/product?filter=name={Uri.EscapeDataString(decodedName)}&limit=1", ct);
        if (data.GetProperty("rows").GetArrayLength() > 0)
            return data.GetProperty("rows")[0].GetProperty("meta").GetProperty("href").GetString();

        // Try fuzzy search — use first 2 meaningful words to match
        var words = decodedName.Replace(",", "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var searchTerm = string.Join(" ", words.Take(Math.Min(2, words.Length)));
        data = await GetAsync($"entity/product?filter=name~={Uri.EscapeDataString(searchTerm)}&limit=1", ct);

        if (data.GetProperty("rows").GetArrayLength() > 0)
            return data.GetProperty("rows")[0].GetProperty("meta").GetProperty("href").GetString();

        return null;
    }

    private async Task<JsonElement> GetAsync(string path, CancellationToken ct)
    {
        var response = await http.GetAsync(path, ct);
        response.EnsureSuccessStatusCode();
        return await ReadResponseAsync(response, ct);
    }

    private static async Task<JsonElement> ReadResponseAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var stream = await response.Content.ReadAsStreamAsync(ct);
        if (response.Content.Headers.ContentEncoding.Contains("gzip"))
            stream = new GZipStream(stream, CompressionMode.Decompress);
        return await JsonSerializer.DeserializeAsync<JsonElement>(stream, cancellationToken: ct);
    }

    private static string BuildDescription(MoySkladOrder order)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Заказ с сайта orehovysad.ru");
        if (!string.IsNullOrEmpty(order.ShippingAddress))
            sb.AppendLine($"Адрес: {order.ShippingAddress}");
        if (!string.IsNullOrEmpty(order.Comment))
            sb.AppendLine($"Комментарий: {order.Comment}");
        if (!string.IsNullOrEmpty(order.PromoCode))
            sb.AppendLine($"Промокод: {order.PromoCode} (скидка {order.Discount}%)");
        sb.AppendLine();
        foreach (var item in order.Items)
            sb.AppendLine($"• {item.ProductName} ({item.Weight}) x{item.Quantity} = {item.Price * item.Quantity} ₽");
        return sb.ToString();
    }

    private static decimal ParseWeightToKg(string weight)
    {
        if (string.IsNullOrWhiteSpace(weight)) return 0;
        var w = weight.Trim();

        // Extract just the leading number before any unit text
        // "500 г" → 500, "1 кг" → 1, "5 кг (-10% за кг 1063р)" → 5
        var match = System.Text.RegularExpressions.Regex.Match(w, @"^([\d.,]+)\s*");
        if (!match.Success) return 0;

        var numStr = match.Groups[1].Value.Replace(',', '.');
        if (!decimal.TryParse(numStr, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var num) || num <= 0)
            return 0;

        var lower = w.ToLowerInvariant();
        if (lower.Contains("кг") || lower.Contains("kg"))
            return num; // already in kg
        if (lower.Contains("г") || lower.Contains("g"))
            return num / 1000; // grams to kg

        return 0;
    }

    private static object[] BuildAttributes(MoySkladOrder order)
    {
        // Return empty — MoySklad custom attributes need to be pre-configured
        // Description field contains all the details
        return [];
    }
}
