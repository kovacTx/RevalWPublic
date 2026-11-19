using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using RevalW.App.Model;
using RevalW.App.Service;

namespace RevalW.App.ViewComponents;

public class MapViewComponent(GoogleGeocodingService geocoder, IConfiguration config) : ViewComponent
{
    private JsonSerializerOptions? _jsonSerializerOptions;
    private JsonSerializerOptions JsonSerializerOptions => _jsonSerializerOptions ??= new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<IViewComponentResult> InvokeAsync(string[]? addresses = null)
    {
        var points = await ReadPoints(addresses);

        var vm = new MapViewModel
        {
            MapId = "map_" + Guid.NewGuid().ToString("N"),
            PointsJson = JsonSerializer.Serialize(points, JsonSerializerOptions),
            ApiKey = config["GoogleMaps:ApiKey"] ?? string.Empty
        };

        return View(vm); // looks for Views/Shared/Components/Map/Default.cshtml
    }

    private async Task<List<AddressPoint>> ReadPoints(IEnumerable<string>? addresses = null)
    {
        if (addresses is null) return [];

        var ret = new List<AddressPoint>();
        foreach (var addr in addresses)
        {
            try
            {
                var coords = await geocoder.GeocodeAsync(addr, HttpContext.RequestAborted);
                if (coords is null) continue;
                ret.Add(new AddressPoint
                {
                    Address = addr,
                    Latitude = coords.Value.lat,
                    Longitude = coords.Value.lng,
                    Label = addr
                });
            }
            catch
            {
                // log in production
            }
        }
        return ret;
    }
}
