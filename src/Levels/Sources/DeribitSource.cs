using System.Globalization;
using System.Text.Json;
using Levels.Options;

namespace Levels.Sources;

/// <summary>Deribit's public API: BTC and ETH option chains and DVOL, no key, real time (spec §6.1).</summary>
internal sealed class DeribitSource(HttpClient http)
{
    private const string BaseUrl = "https://www.deribit.com/api/v2/public/";

    /// <summary>Every option of one currency, as Deribit sends it (the raw text is what gets saved).</summary>
    public Task<string> FetchBookAsync(string currency, CancellationToken ct) =>
        http.GetStringAsync($"{BaseUrl}get_book_summary_by_currency?currency={currency}&kind=option", ct);

    /// <summary>The latest hourly DVOL close (annualised %, 365 days), or null if Deribit has none.</summary>
    public async Task<double?> FetchDvolAsync(string currency, DateTime nowUtc, CancellationToken ct)
    {
        long end = new DateTimeOffset(nowUtc).ToUnixTimeMilliseconds();
        long start = end - (long)TimeSpan.FromHours(6).TotalMilliseconds;
        string json = await http.GetStringAsync(
            $"{BaseUrl}get_volatility_index_data?currency={currency}&start_timestamp={start}&end_timestamp={end}&resolution=3600", ct);
        return LatestDvol(json);
    }

    /// <summary>Rows are [time, open, high, low, close]; the last row's close.</summary>
    internal static double? LatestDvol(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("result").GetProperty("data");
        int n = data.GetArrayLength();
        return n == 0 ? null : data[n - 1][4].GetDouble();
    }

    internal static ChainSnapshot Parse(string currency, string json, DateTime takenUtc)
    {
        using var doc = JsonDocument.Parse(json);
        var options = new List<OptionQuote>();
        double index = 0;
        foreach (var item in doc.RootElement.GetProperty("result").EnumerateArray())
        {
            if (ParseName(item.GetProperty("instrument_name").GetString()) is not { } name)
                continue;
            double underlying = Number(item, "underlying_price") ?? 0;
            double? mark = Number(item, "mark_price");
            if (index == 0)
                index = Number(item, "estimated_delivery_price") ?? 0;
            options.Add(new OptionQuote(name.ExpiryUtc, name.Strike, name.IsCall,
                Number(item, "open_interest") ?? 0,
                mark is null || underlying <= 0 ? null : mark * underlying,
                underlying, Number(item, "mark_iv")));
        }
        return new ChainSnapshot(currency, "Deribit", takenUtc, index, DayCount.Calendar, options);
    }

    /// <summary>
    /// "BTC-31OCT26-87000-C" → expiry 31-Oct-2026 08:00 UTC (Deribit's daily settlement), strike 87,000, call.
    /// A "d" in the strike is a decimal point ("0d625"). Null for any other shape.
    /// </summary>
    internal static (DateTime ExpiryUtc, decimal Strike, bool IsCall)? ParseName(string? name)
    {
        var parts = name?.Split('-');
        if (parts is not { Length: 4 } || parts[3] is not ("C" or "P"))
            return null;
        if (!DateTime.TryParseExact(parts[1], ["dMMMyy", "ddMMMyy"], CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var date))
            return null;
        if (!decimal.TryParse(parts[2].Replace('d', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var strike))
            return null;
        return (DateTime.SpecifyKind(date.Date.AddHours(8), DateTimeKind.Utc), strike, parts[3] == "C");
    }

    private static double? Number(JsonElement item, string field) =>
        item.TryGetProperty(field, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : null;
}
