namespace Nuts.Api.Endpoints;

public static class SettingsEndpoints
{
    private static readonly Dictionary<string, string> _defaults = new()
    {
        ["minOrderAmount"] = "3500",
        ["deliveryCost"] = "500",
        ["freeDeliveryFrom"] = "5000"
    };

    private static Dictionary<string, string> _settings = new(_defaults);

    public static void MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        // Public — get delivery settings
        app.MapGet("/api/settings/delivery", () =>
            TypedResults.Ok(new DeliverySettings(
                decimal.Parse(_settings["minOrderAmount"]),
                decimal.Parse(_settings["deliveryCost"]),
                decimal.Parse(_settings["freeDeliveryFrom"])
            ))).WithTags("Settings");

        // Admin — update delivery settings
        var admin = app.MapGroup("/api/admin/settings")
            .WithTags("Admin — Settings")
            .RequireAuthorization("Admin");

        admin.MapGet("/delivery", () =>
            TypedResults.Ok(new DeliverySettings(
                decimal.Parse(_settings["minOrderAmount"]),
                decimal.Parse(_settings["deliveryCost"]),
                decimal.Parse(_settings["freeDeliveryFrom"])
            )));

        admin.MapPut("/delivery", (DeliverySettings req) =>
        {
            _settings["minOrderAmount"] = req.MinOrderAmount.ToString();
            _settings["deliveryCost"] = req.DeliveryCost.ToString();
            _settings["freeDeliveryFrom"] = req.FreeDeliveryFrom.ToString();
            return TypedResults.Ok(req);
        });
    }
}

public sealed record DeliverySettings(decimal MinOrderAmount, decimal DeliveryCost, decimal FreeDeliveryFrom);
