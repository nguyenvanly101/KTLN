using System.Globalization;
using System.Text.Json;
using HoTroDuLich.Web.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;

namespace HoTroDuLich.Web.Services;

public sealed record OpenMapPlace(string Id, string Name, string Address, string Category, MapPosition Position, string Url);

public sealed class OpenMapPlaces(IHttpClientFactory clients, IMemoryCache cache)
{
    private static readonly Dictionary<string, string[]> TagsByCategory = new()
    {
        ["AnUong"] = ["amenity.restaurant", "amenity.cafe", "amenity.fast_food"],
        ["LuuTru"] = ["tourism.hotel", "tourism.hostel", "tourism.guest_house", "tourism.motel"],
        ["PhuongTien"] = ["amenity.car_rental", "amenity.bicycle_rental", "amenity.motorcycle_rental"],
        ["HoatDong"] = ["tourism.attraction", "tourism.museum", "historic.castle", "historic.monument"],
        ["Parking"] = ["amenity.parking"],
        ["Restroom"] = ["amenity.toilets"],
        ["Fuel"] = ["amenity.fuel"]
    };

    public const int MaxRadiusKm = 5;
    private const string Endpoint = "https://photon.komoot.io/";
    private const string Unavailable = "Chưa lấy được gợi ý OpenStreetMap. Bạn có thể xem cơ sở trên website hoặc đặt ghim.";
    private readonly SemaphoreSlim gate = new(1, 1);
    private DateTimeOffset lastRequest;

    public async Task<List<PlaceSuggestion>> Suggest(string text, CancellationToken ct)
    {
        if (text.Length < 3) return [];
        var results = await Query("api/", [new("q", text), new("limit", "6")], ct);
        return results.Select(p => new PlaceSuggestion(p.Id, p.Name, p.Address, "OpenStreetMap",
            Position: p.Position, MapUrl: p.Url)).ToList();
    }

    public async Task<List<OpenMapPlace>> Nearby(MapPosition center, decimal radius, string category, string? text, CancellationToken ct)
    {
        radius = Math.Min(radius, MaxRadiusKm);
        var args = new List<KeyValuePair<string, string?>>
        {
            new("lat", Coordinate(center.Lat)), new("lon", Coordinate(center.Lng)), new("limit", "25")
        };
        var tags = TagsByCategory.GetValueOrDefault(category)
            ?? (text == null ? TagsByCategory.Values.SelectMany(group => group).ToArray() : []);
        if (tags.Length > 0)
            args.Add(new("include", string.Join(",", tags.Select(tag => "osm." + tag))));

        if (text == null)
        {
            args.Add(new("radius", Coordinate(radius)));
        }
        else
        {
            var latOffset = radius / 110m;
            var lngOffset = radius / (111m * Math.Max(0.01m, (decimal)Math.Cos((double)center.Lat * Math.PI / 180)));
            args.Add(new("q", text));
            args.Add(new("bbox", string.Join(",", new[]
            {
                Coordinate(Math.Max(-180, center.Lng - lngOffset)), Coordinate(Math.Max(-90, center.Lat - latOffset)),
                Coordinate(Math.Min(180, center.Lng + lngOffset)), Coordinate(Math.Min(90, center.Lat + latOffset))
            })));
        }

        var results = await Query(text == null ? "reverse/" : "api/", args, ct);
        return results.Where(p => (category.Length == 0 || p.Category == category)
            && PlaceSearchService.Distance(center, p.Position) <= (double)radius).ToList();
    }

    private async Task<List<OpenMapPlace>> Query(string path, List<KeyValuePair<string, string?>> args, CancellationToken ct)
    {
        if (path == "api/") args.Add(new("countrycode", "VN"));
        var url = QueryHelpers.AddQueryString(Endpoint + path, args);
        if (cache.TryGetValue(url, out List<OpenMapPlace>? saved)) return saved!;
        if (!await gate.WaitAsync(TimeSpan.FromSeconds(2), ct))
            throw new BusinessException(Unavailable, 503);
        try
        {
            if (cache.TryGetValue(url, out saved)) return saved!;
            var pause = lastRequest.AddMilliseconds(1100) - DateTimeOffset.UtcNow;
            if (pause > TimeSpan.Zero) await Task.Delay(pause, ct);
            lastRequest = DateTimeOffset.UtcNow;

            using var response = await clients.CreateClient("OpenMapPlaces").GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var results = doc.RootElement.GetProperty("features").EnumerateArray()
                .Select(ReadPlace).OfType<OpenMapPlace>().DistinctBy(place => place.Id).ToList();
            cache.Set(url, results, TimeSpan.FromMinutes(5));
            return results;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new BusinessException(Unavailable, 503);
        }
        catch (Exception error) when (error is HttpRequestException or JsonException or InvalidOperationException or KeyNotFoundException)
        {
            throw new BusinessException(Unavailable, 503);
        }
        finally
        {
            gate.Release();
        }
    }

    private static OpenMapPlace? ReadPlace(JsonElement feature)
    {
        var properties = feature.GetProperty("properties");
        if (!string.Equals(Text(properties, "countrycode"), "VN", StringComparison.OrdinalIgnoreCase)) return null;
        var kind = Text(properties, "osm_type") switch { "N" => "node", "W" => "way", "R" => "relation", _ => null };
        if (kind == null || !properties.TryGetProperty("osm_id", out var id) || !id.TryGetInt64(out var osmId) || osmId <= 0)
            return null;

        var coords = feature.GetProperty("geometry").GetProperty("coordinates");
        if (coords.GetArrayLength() < 2 || !coords[0].TryGetDecimal(out var lng) || !coords[1].TryGetDecimal(out var lat)
            || lat is < -90 or > 90 || lng is < -180 or > 180) return null;

        var street = string.Join(" ", new[] { Text(properties, "housenumber"), Text(properties, "street") }
            .Where(part => part != null));
        var address = string.Join(", ", new[] { street, Text(properties, "district"), Text(properties, "city"),
            Text(properties, "county"), Text(properties, "state") }.Where(part => !string.IsNullOrWhiteSpace(part))
            .Distinct(StringComparer.OrdinalIgnoreCase));
        var tag = Text(properties, "osm_key") + "." + Text(properties, "osm_value");
        var category = TagsByCategory.FirstOrDefault(pair => pair.Value.Contains(tag)).Key ?? "";
        var name = Text(properties, "name");
        if (name == null && category is "Parking" or "Restroom" or "Fuel")
            name = PlaceText.Categories[category];
        if (name == null && category == "" && street.Length > 0) name = street;
        if (name == null) return null;

        var objectPath = kind + "/" + osmId.ToString(CultureInfo.InvariantCulture);
        return new(objectPath, name[..Math.Min(200, name.Length)], address, category, new(lat, lng),
            "https://www.openstreetmap.org/" + objectPath);
    }

    private static string? Text(JsonElement data, string key) =>
        data.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String
        && !string.IsNullOrWhiteSpace(value.GetString()) ? value.GetString()!.Trim() : null;

    private static string Coordinate(decimal value) => value.ToString("0.######", CultureInfo.InvariantCulture);
}
