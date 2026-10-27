using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using Levels.Options;

namespace Levels.Sources;

/// <summary>
/// Tradier's market-data API for US options (SPY): quote, expiry list, one chain per expiry with greeks (spec §6.1).
/// Needs the owner's token; the free developer sandbox is delayed 15 minutes.
/// </summary>
internal sealed class TradierSource(HttpClient http, string baseUrl, string token)
{
    public const string SandboxUrl = "https://sandbox.tradier.com/v1/";

    /// <summary>Chains are fetched for expiries this many days ahead, at most <see cref="MaxExpiries"/> of them.</summary>
    internal const int ExpiryDays = 60;
    internal const int MaxExpiries = 30;
    internal static readonly TimeSpan Pause = TimeSpan.FromSeconds(1);

    /// <summary>Everything fetched, in one object: {"quote": …, "expirations": …, "chains": {"2026-10-28": …}}.</summary>
    public async Task<JsonObject> FetchAsync(string symbol, DateTime nowUtc, CancellationToken ct)
    {
        var raw = new JsonObject
        {
            ["quote"] = await GetAsync($"markets/quotes?symbols={symbol}", ct),
        };
        var expirations = await GetAsync($"markets/options/expirations?symbol={symbol}", ct);
        raw["expirations"] = expirations;

        var chains = new JsonObject();
        foreach (var date in SelectExpiries(Items(expirations?["expirations"]?["date"]).Select(d => d!.GetValue<string>()), nowUtc))
        {
            await Task.Delay(Pause, ct);
            chains[date] = await GetAsync($"markets/options/chains?symbol={symbol}&expiration={date}&greeks=true", ct);
        }
        raw["chains"] = chains;
        return raw;
    }

    private async Task<JsonNode?> GetAsync(string path, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, baseUrl.TrimEnd('/') + "/" + path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await http.SendAsync(request, ct);
        string body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Tradier {path}: {(int)response.StatusCode} {response.ReasonPhrase}: {body.Trim()}");
        return JsonNode.Parse(body);
    }

    /// <summary>Expiries from today (New York date) up to <see cref="ExpiryDays"/> ahead, nearest first, at most <see cref="MaxExpiries"/>.</summary>
    internal static IReadOnlyList<string> SelectExpiries(IEnumerable<string> dates, DateTime nowUtc)
    {
        var today = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, ChainSummary.NewYork).Date;
        return dates
            .Select(d => (Text: d, Date: DateTime.ParseExact(d, "yyyy-MM-dd", CultureInfo.InvariantCulture)))
            .Where(d => d.Date >= today && d.Date <= today.AddDays(ExpiryDays))
            .OrderBy(d => d.Date)
            .Take(MaxExpiries)
            .Select(d => d.Text)
            .ToList();
    }

    internal static ChainSnapshot Parse(string symbol, JsonNode raw, DateTime takenUtc)
    {
        var quote = Items(raw["quote"]?["quotes"]?["quote"]).FirstOrDefault();
        double price = Number(quote, "last") ?? Mid(Number(quote, "bid"), Number(quote, "ask")) ?? 0;

        var options = new List<OptionQuote>();
        foreach (var (_, chain) in raw["chains"]?.AsObject() ?? [])
        {
            foreach (var o in Items(chain?["options"]?["option"]))
            {
                if (o?["expiration_date"]?.GetValue<string>() is not { } date || Number(o, "strike") is not { } strike)
                    continue;
                var type = o["option_type"]?.GetValue<string>();
                if (type is not ("call" or "put"))
                    continue;
                double? last = Number(o, "last");
                double? iv = Number(o["greeks"], "mid_iv");
                options.Add(new OptionQuote(
                    ExpiryUtc(date), (decimal)strike, type == "call",
                    Number(o, "open_interest") ?? 0,
                    Mid(Number(o, "bid"), Number(o, "ask")) ?? (last > 0 ? last : null),
                    price,
                    iv > 0 ? iv * 100 : null));
            }
        }
        return new ChainSnapshot(symbol, "Tradier", takenUtc, price, DayCount.TradingSessions, options);
    }

    /// <summary>A US equity option expires at 16:00 New York on its expiry date.</summary>
    internal static DateTime ExpiryUtc(string date) => TimeZoneInfo.ConvertTimeToUtc(
        DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture).AddHours(16), ChainSummary.NewYork);

    /// <summary>Tradier sends a single item as an object and several as an array; null or "null" as none.</summary>
    internal static IEnumerable<JsonNode?> Items(JsonNode? node) => node switch
    {
        null => Array.Empty<JsonNode?>(),
        JsonArray array => array,
        JsonValue v when v.GetValueKind() == System.Text.Json.JsonValueKind.Null => Array.Empty<JsonNode?>(),
        _ => new[] { node },   // not a collection expression: as a JsonArray it would try to re-parent the node
    };

    private static double? Mid(double? bid, double? ask) => bid > 0 && ask > 0 ? (bid + ask) / 2 : null;

    private static double? Number(JsonNode? node, string field) =>
        node?[field] is JsonValue v && v.GetValueKind() == System.Text.Json.JsonValueKind.Number ? v.GetValue<double>() : null;
}
