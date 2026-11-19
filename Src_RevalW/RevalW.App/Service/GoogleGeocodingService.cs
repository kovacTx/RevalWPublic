using Microsoft.Extensions.Caching.Memory;
using System.Net;

namespace RevalW.App.Service;

public class GoogleGeocodingService(IHttpClientFactory httpFactory, IMemoryCache cache, IConfiguration configuration)
{
    private readonly string apiKey = configuration.GetValue<string>("GoogleMaps:ApiKey") ?? string.Empty;
    private readonly TimeSpan cacheDuration = TimeSpan.FromDays(30);

    public async Task<(double lat, double lng)?> GeocodeAsync(string address, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address) || string.IsNullOrEmpty(apiKey)) return null;

        var cacheKey = $"geocode:{address.Trim().ToLowerInvariant()}";
        if (cache.TryGetValue(cacheKey, out (double lat, double lng) cached))
            return cached;

        var client = httpFactory.CreateClient("google");
        var url = $"maps/api/geocode/json?address={WebUtility.UrlEncode(address)}&key={apiKey}";

        var resp = await client.GetFromJsonAsync<GeocodeResponse>(url, cancellationToken);
        if (resp?.status == "OK" && resp.results?.Length > 0)
        {
            var loc = resp.results[0].geometry.location;
            var result = (loc.lat, loc.lng);
            cache.Set(cacheKey, result, cacheDuration);
            return result;
        }

        return null;
    }

    private class GeocodeResponse
    {
        public string? status { get; set; }
        public GeocodeResult[]? results { get; set; }
    }

    private class GeocodeResult
    {
        public Geometry geometry { get; set; } = new Geometry();
    }

    private class Geometry
    {
        public Location location { get; set; } = new Location();
    }

    private class Location
    {
        public double lat { get; set; }
        public double lng { get; set; }
    }
}
