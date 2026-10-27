using Levels.Options;

namespace Levels.Tests;

public class ChainSummaryTests
{
    private static readonly DateTime Now = new(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Soon = Now.AddHours(6);     // too close for the straddle
    private static readonly DateTime Next = Now.AddDays(4);
    private static readonly DateTime Later = Now.AddDays(30);

    private static OptionQuote Q(DateTime expiry, decimal strike, bool call, double oi, double? mid = null, double underlying = 100, double? iv = null) =>
        new(expiry, strike, call, oi, mid, underlying, iv);

    private static ChainSnapshot Chain(params OptionQuote[] options) => new("TEST", "test", Now, 100, DayCount.Calendar, options);

    [Fact]
    public void Put_call_balance_and_top_expiries()
    {
        var s = ChainSummary.From(Chain(
            Q(Next, 100, true, 30), Q(Next, 100, false, 10),
            Q(Later, 110, true, 40), Q(Later, 90, false, 20)));

        Assert.Equal(70, s.CallOi);
        Assert.Equal(30, s.PutOi);
        Assert.Equal([Later, Next], s.TopExpiries.Select(e => e.ExpiryUtc));
        Assert.Equal(0.6, s.TopExpiries[0].Share, 9);
    }

    [Fact]
    public void Top_strikes_add_up_all_expiries_and_skip_empty_ones()
    {
        var s = ChainSummary.From(Chain(
            Q(Next, 100, true, 30), Q(Later, 100, true, 25),     // 100: 55
            Q(Later, 110, true, 40),
            Q(Later, 120, true, 0),
            Q(Next, 90, false, 20), Q(Later, 80, false, 20)));   // tie → lower strike first

        Assert.Equal([(100m, 55.0), (110m, 40.0)], s.TopCallStrikes.Select(x => (x.Strike, x.OpenInterest)));
        Assert.Equal([80m, 90m], s.TopPutStrikes.Select(x => x.Strike));
    }

    [Fact]
    public void Straddle_sigma_hand_worked()
    {
        // Price 100, call 4 + put 4 = 8 → σ to expiry = 8 × √(π/2) ÷ 100 = 10.03 %; 4 days → 5.01 % a day.
        // IV 40 % → 40 ÷ 100 ÷ √365 = 2.09 % a day.
        var st = ChainSummary.FindStraddle(Chain(
            Q(Next, 95, true, 1, 7), Q(Next, 95, false, 1, 2),
            Q(Next, 100, true, 1, 4, iv: 40), Q(Next, 100, false, 1, 4, iv: 40),
            Q(Next, 105, true, 1, 2), Q(Next, 105, false, 1, 7)))!;

        Assert.Equal(100m, st.Strike);
        Assert.Equal(4, st.Days, 9);
        Assert.Equal(0.1002651, st.SigmaToExpiry, 6);
        Assert.Equal(0.0501326, st.SigmaPerDay, 6);
        Assert.Equal(0.0209370, st.IvSigmaPerDay!.Value, 6);
    }

    [Fact]
    public void Straddle_skips_expiries_closer_than_12_hours_and_strikes_without_both_prices()
    {
        var st = ChainSummary.FindStraddle(Chain(
            Q(Soon, 100, true, 1, 1), Q(Soon, 100, false, 1, 1),
            Q(Next, 100, true, 1, 4), Q(Next, 100, false, 1, null),   // no put price at 100
            Q(Next, 102, true, 1, 3), Q(Next, 102, false, 1, 5)))!;

        Assert.Equal(Next, st.ExpiryUtc);
        Assert.Equal(102m, st.Strike);
    }

    [Fact]
    public void No_straddle_without_a_usable_expiry() =>
        Assert.Null(ChainSummary.FindStraddle(Chain(Q(Soon, 100, true, 1, 1), Q(Soon, 100, false, 1, 1))));

    [Fact]
    public void Trading_sessions_count_weekdays_and_the_rest_of_an_open_session()
    {
        // Tue 27-Oct-2026 20:00 UTC = 16:00 New York (closed) → Wed, Thu, Fri = 3 sessions to Friday's close.
        var friClose = new DateTime(2026, 10, 30, 20, 0, 0, DateTimeKind.Utc);
        Assert.Equal(3, ChainSummary.TradingSessions(new DateTime(2026, 10, 27, 20, 0, 0, DateTimeKind.Utc), friClose), 9);

        // Before the open on Tue (08:00 New York) → Tue to Fri = 4.
        Assert.Equal(4, ChainSummary.TradingSessions(new DateTime(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc), friClose), 9);

        // Fri 12:45 New York (16:45 UTC): 3.25 h of 6.5 left → 0.5.
        Assert.Equal(0.5, ChainSummary.TradingSessions(new DateTime(2026, 10, 30, 16, 45, 0, DateTimeKind.Utc), friClose), 9);

        // Friday after the close to next Monday's close: the weekend counts nothing.
        Assert.Equal(1, ChainSummary.TradingSessions(friClose, new DateTime(2026, 11, 2, 21, 0, 0, DateTimeKind.Utc)), 9);
    }
}
