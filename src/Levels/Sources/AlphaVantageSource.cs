using System.Globalization;
using System.Text.Json.Nodes;
using Levels.Options;

namespace Levels.Sources;

/// <summary>
/// Alpha Vantage for US options (SPY): the previous session's whole chain with OI, IV and greeks, and the daily
/// closes for the price on that date (spec §6.1). Needs the owner's free key; the free plan allows 25 requests a day.
/// </summary>
internal sealed class AlphaVantageSource(HttpClient http, string key)
{
    public const string BaseUrl = "https://www.alphavantage.co/query";
    internal static readonly TimeSpan Pause = TimeSpan.FromSeconds(2);

    /// <summary>Two requests, in one object: {"options": HISTORICAL_OPTIONS, "daily": TIME_SERIES_DAILY}.</summary>
    public async Task<JsonObject> FetchAsync(string symbol, CancellationToken ct)
    {
        var options = await GetAsync($"function=HISTORICAL_OPTIONS&symbol={symbol}", ct);
        await Task.Delay(Pause, ct);
        var daily = await GetAsync($"function=TIME_SERIES_DAILY&symbol={symbol}", ct);
        return new JsonObject { ["options"] = options, ["daily"] = daily };
    }

    private async Task<JsonNode?> GetAsync(string query, CancellationToken ct)
    {
        // The key is a query parameter; it is never put in a message.
        using var response = await http.GetAsync($"{BaseUrl}?{query}&apikey={Uri.EscapeDataString(key)}", ct);
        string body = await response.Content.ReadAsStringAsync(ct);
        string function = query.Split('&')[0];
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Alpha Vantage {function}: {(int)response.StatusCode} {response.ReasonPhrase}");
        var node = JsonNode.Parse(body);
        if (Refusal(node) is { } refusal)
            throw new HttpRequestException($"Alpha Vantage {function}: {refusal}");
        return node;
    }

    /// <summary>Alpha Vantage answers a bad key, a wrong symbol or the daily limit with HTTP 200 and one of these fields.</summary>
    internal static string? Refusal(JsonNode? node) =>
        node is JsonObject o && (o["Error Message"] ?? o["Information"] ?? o["Note"]) is JsonValue v ? v.GetValue<string>() : null;

    internal static ChainSnapshot Parse(string symbol, JsonNode raw)
    {
        var rows = raw["options"]?["data"]?.AsArray() ?? [];
        string date = rows.Select(r => r?["date"]?.GetValue<string>()).FirstOrDefault(d => d is not null)
            ?? throw new FormatException("no options in the answer");
        double price = Number(raw["daily"]?["Time Series (Daily)"]?[date], "4. close")
            ?? throw new FormatException($"no daily close for {date}");

        var options = new List<OptionQuote>();
        foreach (var o in rows)
        {
            if (o?["expiration"]?.GetValue<string>() is not { } expiry || Number(o, "strike") is not { } strike)
                continue;
            var type = o["type"]?.GetValue<string>();
            if (type is not ("call" or "put"))
                continue;
            double? bid = Number(o, "bid"), ask = Number(o, "ask"), mark = Number(o, "mark"), last = Number(o, "last");
            double? iv = Number(o, "implied_volatility");
            options.Add(new OptionQuote(
                UsClose(expiry), (decimal)strike, type == "call",
                Number(o, "open_interest") ?? 0,
                bid > 0 && ask > 0 ? (bid + ask) / 2 : mark > 0 ? mark : last > 0 ? last : null,
                price,
                iv > 0 ? iv * 100 : null));
        }
        // The chain is the close of its date, so time to expiry is counted from there, not from when it was fetched.
        return new ChainSnapshot(symbol, "Alpha Vantage", UsClose(date), price, DayCount.TradingSessions, options);
    }

    /// <summary>16:00 New York on a date: the US close, and when a US equity option expires.</summary>
    internal static DateTime UsClose(string date) => TimeZoneInfo.ConvertTimeToUtc(
        DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture).AddHours(16), ChainSummary.NewYork);

    /// <summary>Alpha Vantage sends numbers as strings ("745.00"); a real JSON number is read too.</summary>
    private static double? Number(JsonNode? node, string field) => node?[field] switch
    {
        JsonValue v when v.TryGetValue<string>(out var s) =>
            double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ? x : null,
        JsonValue v when v.TryGetValue<double>(out var x) => x,
        _ => null,
    };
}
