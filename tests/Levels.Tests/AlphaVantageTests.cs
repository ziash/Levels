using System.Text.Json.Nodes;
using Levels.Options;
using Levels.Sources;

namespace Levels.Tests;

public class AlphaVantageTests
{
    [Fact]
    public void Us_close_is_1600_new_york()
    {
        Assert.Equal(new DateTime(2026, 10, 28, 20, 0, 0, DateTimeKind.Utc), AlphaVantageSource.UsClose("2026-10-28"));   // EDT
        Assert.Equal(new DateTime(2026, 11, 20, 21, 0, 0, DateTimeKind.Utc), AlphaVantageSource.UsClose("2026-11-20"));   // EST
    }

    [Fact]
    public void Refusals_are_found()
    {
        Assert.Equal("limit", AlphaVantageSource.Refusal(JsonNode.Parse("""{"Information":"limit"}""")));
        Assert.Equal("bad", AlphaVantageSource.Refusal(JsonNode.Parse("""{"Error Message":"bad"}""")));
        Assert.Null(AlphaVantageSource.Refusal(JsonNode.Parse("""{"endpoint":"Historical Options","message":"success","data":[]}""")));
    }

    [Fact]
    public void Chain_parses_strings_and_takes_the_close_of_its_date()
    {
        var raw = JsonNode.Parse("""
            {"options":{"endpoint":"Historical Options","message":"success","data":[
               {"contractID":"SPY261028C00745000","expiration":"2026-10-28","strike":"745.00","type":"call","last":"3.00","mark":"3.20","bid":"3.10","ask":"3.30","open_interest":"1200","date":"2026-10-27","implied_volatility":"0.14520"},
               {"contractID":"SPY261028P00745000","expiration":"2026-10-28","strike":"745.00","type":"put","last":"2.70","mark":"2.80","bid":"0.00","ask":"0.00","open_interest":"900","date":"2026-10-27","implied_volatility":"0.00000"},
               {"contractID":"SPY261030P00700000","expiration":"2026-10-30","strike":"700.00","type":"put","last":"0.00","mark":"0.00","bid":"0.00","ask":"0.00","open_interest":"0","date":"2026-10-27","implied_volatility":"0.31"}
             ]},
             "daily":{"Time Series (Daily)":{
               "2026-10-27":{"1. open":"744.0","4. close":"745.20"},
               "2026-10-26":{"1. open":"740.0","4. close":"743.10"}}}}
            """)!;

        var chain = AlphaVantageSource.Parse("SPY", raw);

        Assert.Equal(745.2, chain.Price);                                                    // close of 27-Oct, not 26-Oct
        Assert.Equal(new DateTime(2026, 10, 27, 20, 0, 0, DateTimeKind.Utc), chain.TakenUtc);
        Assert.Equal(DayCount.TradingSessions, chain.Days);
        Assert.Equal(3, chain.Options.Count);
        var call = chain.Options[0];
        Assert.True(call.IsCall);
        Assert.Equal(745m, call.Strike);
        Assert.Equal(1200, call.OpenInterest);
        Assert.Equal(3.2, call.Mid!.Value, 9);           // bid/ask mid
        Assert.Equal(14.52, call.Iv!.Value, 9);          // 0.1452 → percent
        var put = chain.Options[1];
        Assert.Equal(2.8, put.Mid);                      // no bid/ask → mark
        Assert.Null(put.Iv);
        Assert.Null(chain.Options[2].Mid);               // no price at all
    }

    [Fact]
    public void A_chain_without_a_matching_close_is_refused() =>
        Assert.Throws<FormatException>(() => AlphaVantageSource.Parse("SPY", JsonNode.Parse("""
            {"options":{"data":[{"expiration":"2026-10-28","strike":"745","type":"call","date":"2026-10-27"}]},
             "daily":{"Time Series (Daily)":{"2026-10-26":{"4. close":"743.10"}}}}
            """)!));
}
