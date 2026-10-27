using System.Text.Json.Nodes;
using Levels.Options;
using Levels.Sources;

namespace Levels.Tests;

public class TradierTests
{
    private static readonly DateTime Now = new(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);   // 08:00 New York

    [Fact]
    public void Expiry_is_1600_new_york()
    {
        Assert.Equal(new DateTime(2026, 10, 28, 20, 0, 0, DateTimeKind.Utc), TradierSource.ExpiryUtc("2026-10-28"));   // EDT
        Assert.Equal(new DateTime(2026, 11, 20, 21, 0, 0, DateTimeKind.Utc), TradierSource.ExpiryUtc("2026-11-20"));   // EST
    }

    [Fact]
    public void Expiries_from_today_up_to_60_days_nearest_first()
    {
        var picked = TradierSource.SelectExpiries(
            ["2026-12-31", "2026-10-26", "2026-10-27", "2026-10-28", "2026-12-26", "2027-01-15"], Now);
        Assert.Equal(["2026-10-27", "2026-10-28", "2026-12-26"], picked);
    }

    [Fact]
    public void At_most_30_expiries()
    {
        var dates = Enumerable.Range(0, 50).Select(i => new DateTime(2026, 10, 27).AddDays(i).ToString("yyyy-MM-dd"));
        Assert.Equal(TradierSource.MaxExpiries, TradierSource.SelectExpiries(dates, Now).Count);
    }

    [Fact]
    public void Single_items_arrays_and_nulls_are_all_lists()
    {
        Assert.Single(TradierSource.Items(JsonNode.Parse("""{"a":1}""")));
        Assert.Equal(2, TradierSource.Items(JsonNode.Parse("[1,2]")).Count());
        Assert.Empty(TradierSource.Items(null));
        Assert.Empty(TradierSource.Items(JsonNode.Parse("""{"x":null}""")!["x"]));
    }

    [Fact]
    public void Chain_parses_quotes_options_and_iv()
    {
        var raw = JsonNode.Parse("""
            {"quote":{"quotes":{"quote":{"symbol":"SPY","last":745.2,"bid":745.1,"ask":745.3}}},
             "expirations":{"expirations":{"date":["2026-10-28"]}},
             "chains":{
               "2026-10-28":{"options":{"option":[
                 {"symbol":"SPY261028C00745000","strike":745.0,"option_type":"call","open_interest":1200,"bid":3.1,"ask":3.3,"last":3.0,"expiration_date":"2026-10-28","greeks":{"mid_iv":0.1452}},
                 {"symbol":"SPY261028P00745000","strike":745.0,"option_type":"put","open_interest":900,"bid":0,"ask":2.9,"last":2.7,"expiration_date":"2026-10-28","greeks":null}
               ]}},
               "2026-10-30":{"options":null}
             }}
            """)!;

        var chain = TradierSource.Parse("SPY", raw, Now);

        Assert.Equal(745.2, chain.Price);
        Assert.Equal(DayCount.TradingSessions, chain.Days);
        Assert.Equal(2, chain.Options.Count);
        var call = chain.Options[0];
        Assert.True(call.IsCall);
        Assert.Equal(745m, call.Strike);
        Assert.Equal(3.2, call.Mid!.Value, 9);           // bid/ask mid
        Assert.Equal(14.52, call.Iv!.Value, 9);          // 0.1452 → percent
        Assert.Equal(new DateTime(2026, 10, 28, 20, 0, 0, DateTimeKind.Utc), call.ExpiryUtc);
        var put = chain.Options[1];
        Assert.Equal(2.7, put.Mid);                      // no bid → last trade
        Assert.Null(put.Iv);
    }
}
