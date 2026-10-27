using Levels.Options;
using Levels.Sources;

namespace Levels.Tests;

public class DeribitTests
{
    private static DateTime Utc(int y, int m, int d, int h = 0, int min = 0) => new(y, m, d, h, min, 0, DateTimeKind.Utc);

    [Fact]
    public void Name_gives_expiry_at_0800_utc_strike_and_type()
    {
        Assert.Equal((Utc(2026, 10, 31, 8), 87000m, true), DeribitSource.ParseName("BTC-31OCT26-87000-C"));
        Assert.Equal((Utc(2026, 11, 5, 8), 3500m, false), DeribitSource.ParseName("ETH-5NOV26-3500-P"));
        Assert.Equal((Utc(2026, 12, 25, 8), 0.625m, true), DeribitSource.ParseName("XRP_USDC-25DEC26-0d625-C"));
    }

    [Theory]
    [InlineData("BTC-PERPETUAL")]
    [InlineData("BTC-31OCT26")]
    [InlineData("BTC-31XYZ26-87000-C")]
    [InlineData("BTC-31OCT26-87000-X")]
    [InlineData(null)]
    public void Other_names_are_ignored(string? name) => Assert.Null(DeribitSource.ParseName(name));

    private const string Book = """
        {"jsonrpc":"2.0","result":[
          {"instrument_name":"BTC-28OCT26-80000-C","open_interest":120.5,"mark_price":0.01,"underlying_price":80500,"estimated_delivery_price":80400,"mark_iv":40.0},
          {"instrument_name":"BTC-28OCT26-80000-P","open_interest":80,"mark_price":0.005,"underlying_price":80500,"estimated_delivery_price":80400,"mark_iv":42.0},
          {"instrument_name":"BTC-PERPETUAL","open_interest":1},
          {"instrument_name":"BTC-27NOV26-90000-C","open_interest":300,"mark_price":null,"underlying_price":81000,"estimated_delivery_price":80400}
        ]}
        """;

    [Fact]
    public void Book_parses_into_quotes_in_usd()
    {
        var chain = DeribitSource.Parse("BTC", Book, Utc(2026, 10, 27, 12));

        Assert.Equal(80400, chain.Price);
        Assert.Equal(DayCount.Calendar, chain.Days);
        Assert.Equal(3, chain.Options.Count);
        var call = chain.Options[0];
        Assert.Equal(805, call.Mid!.Value, 6);          // 0.01 BTC × 80,500
        Assert.Equal(120.5, call.OpenInterest);
        Assert.Equal(40.0, call.Iv);
        Assert.Null(chain.Options[2].Mid);              // no mark price
    }

    [Fact]
    public void Latest_dvol_is_the_last_rows_close()
    {
        const string json = """{"result":{"data":[[1,37.38,37.4,37.25,37.28],[2,37.55,37.56,37.51,37.51]],"continuation":null}}""";
        Assert.Equal(37.51, DeribitSource.LatestDvol(json));
        Assert.Null(DeribitSource.LatestDvol("""{"result":{"data":[]}}"""));
    }
}
